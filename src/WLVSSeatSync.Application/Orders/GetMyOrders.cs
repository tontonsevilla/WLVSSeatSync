using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Common;

namespace WLVSSeatSync.Application.Orders;

public sealed record GetMyOrdersQuery(Guid CustomerId) : IRequest<IReadOnlyList<OrderSummary>>;

public sealed record OrderSummary(Guid Id, Guid EventId, string EventTitle, DateTimeOffset EventStartsAt,
    string Status, decimal Total, string Currency, int SeatCount, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

internal sealed class GetMyOrdersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMyOrdersQuery, IReadOnlyList<OrderSummary>>
{
    public async Task<IReadOnlyList<OrderSummary>> Handle(GetMyOrdersQuery request,
        CancellationToken cancellationToken) =>
        await (
                from o in db.Orders.AsNoTracking()
                join e in db.Events on o.EventId equals e.Id
                where o.CustomerId == request.CustomerId
                orderby o.CreatedAt descending
                select new OrderSummary(
                    o.Id, e.Id, e.Title, e.StartsAt,
                    o.Status.ToString(),
                    o.Items.Sum(i => i.Price.Amount),
                    o.Items.Select(i => i.Price.Currency).FirstOrDefault() ?? Money.Php,
                    o.Items.Count(),
                    o.CreatedAt, o.ExpiresAt))
            .Take(50)
            .ToListAsync(cancellationToken);
}
