using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Contracts;
using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Orders;
using WLVSSeatSync.Domain.Tickets;

namespace WLVSSeatSync.Application.Orders;

public sealed class OrderPaidMessageFactory(IApplicationDbContext db, IUserDirectory users)
{
    public async Task<OrderPaid> CreateAsync(Order order, Event ev, IReadOnlyList<Ticket> tickets,
        DateTimeOffset paidAt, CancellationToken cancellationToken)
    {
        var customer = await users.FindAsync(order.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer {order.CustomerId} has no email address.");

        var venueName = await db.Venues
            .Where(v => v.Id == ev.VenueId)
            .Select(v => v.Name)
            .FirstAsync(cancellationToken);

        var seatIds = tickets.Select(t => t.EventSeatId).ToList();
        var positions = await (
                from eventSeat in db.Events.SelectMany(e => e.Seats)
                join seat in db.Venues.SelectMany(v => v.Seats) on eventSeat.SeatId equals seat.Id
                where seatIds.Contains(eventSeat.Id)
                select new { eventSeat.Id, seat.Position.Section, seat.Position.Row, seat.Position.Number })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var paidTickets = tickets
            .Select(t =>
            {
                var p = positions[t.EventSeatId];
                return new PaidTicket(t.Code, p.Section, p.Row, p.Number);
            })
            .ToList();

        return new OrderPaid(order.Id, order.CustomerId, customer.Email, customer.DisplayName,
            ev.Id, ev.Title, ev.StartsAt, venueName,
            order.Total.Amount, order.Total.Currency, paidTickets, paidAt);
    }
}