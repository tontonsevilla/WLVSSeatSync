using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Orders;

namespace WLVSSeatSync.Application.Orders;

public sealed record GetOrderByIdQuery(Guid CustomerId, Guid OrderId) : IRequest<Result<OrderDetails>>;

public sealed record OrderDetails(Guid Id, Guid EventId, string EventTitle, string Status, decimal Total,
    string Currency, DateTimeOffset ExpiresAt, DateTimeOffset? PaidAt,
    IReadOnlyList<OrderSeat> Seats, IReadOnlyList<OrderTicket> Tickets);

public sealed record OrderSeat(Guid EventSeatId, string Section, string Row, int Number, decimal Price);

public sealed record OrderTicket(string Code, Guid EventSeatId, string Status);

internal sealed class GetOrderByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDetails>>
{
    public async Task<Result<OrderDetails>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == request.CustomerId,
                cancellationToken);

        if (order is null)
            return OrderErrors.NotFound(request.OrderId);

        var eventTitle = await db.Events
            .Where(e => e.Id == order.EventId)
            .Select(e => e.Title)
            .FirstAsync(cancellationToken);

        // Event seat -> venue seat, to show section, row and number.
        var seatIds = order.GetEventSeatIds();
        var positions = await (
                from eventSeat in db.Events.SelectMany(e => e.Seats)
                join seat in db.Venues.SelectMany(v => v.Seats) on eventSeat.SeatId equals seat.Id
                where seatIds.Contains(eventSeat.Id)
                select new { eventSeat.Id, seat.Position.Section, seat.Position.Row, seat.Position.Number })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var tickets = await db.Tickets
            .AsNoTracking()
            .Where(t => t.OrderId == order.Id)
            .Select(t => new OrderTicket(t.Code, t.EventSeatId, t.Status.ToString()))
            .ToListAsync(cancellationToken);

        var seats = order.Items
            .Select(item =>
            {
                var p = positions[item.EventSeatId];
                return new OrderSeat(item.EventSeatId, p.Section, p.Row, p.Number, item.Price.Amount);
            })
            .OrderBy(s => s.Section).ThenBy(s => s.Row).ThenBy(s => s.Number)
            .ToList();

        return new OrderDetails(order.Id, order.EventId, eventTitle, order.Status.ToString(),
            order.Total.Amount, order.Total.Currency, order.ExpiresAt, order.PaidAt, seats, tickets);
    }
}
