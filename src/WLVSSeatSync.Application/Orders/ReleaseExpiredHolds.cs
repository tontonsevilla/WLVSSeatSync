using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Application.Common;
using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Orders;

namespace WLVSSeatSync.Application.Orders;

public sealed record ReleaseExpiredHoldsCommand : IRequest<ReleaseExpiredHoldsResult>;

public sealed record ReleaseExpiredHoldsResult(int SeatsReleased, int OrdersExpired);

internal sealed class ReleaseExpiredHoldsCommandHandler(IApplicationDbContext db, 
    TimeProvider clock, 
    SeatMapUpdates seatMapUpdates)
    : IRequestHandler<ReleaseExpiredHoldsCommand, ReleaseExpiredHoldsResult>
{
    public async Task<ReleaseExpiredHoldsResult> Handle(ReleaseExpiredHoldsCommand request,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        // Only events that have expired holds, and only those seats.
        var events = await db.Events
            .Include(e => e.Seats.Where(s => s.Status == SeatStatus.Held && s.HeldUntil <= now))
            .Where(e => e.Seats.Any(s => s.Status == SeatStatus.Held && s.HeldUntil <= now))
            .ToListAsync(cancellationToken);

        var seatsReleased = 0;
        var released = new List<(Guid EventId, List<Guid> SeatIds)>();
        foreach (var ev in events)
        {
            var seatIds = ev.Seats.Select(s => s.Id).ToList(); // the filtered Include loaded only expired seats
            seatsReleased += ev.ReleaseExpiredHolds(now);
            released.Add((ev.Id, seatIds));
        }

        var orders = await db.Orders
            .Where(o => o.Status == OrderStatus.Pending && o.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
            order.Expire(now);

        await db.SaveChangesAsync(cancellationToken);

        foreach (var (eventId, seatIds) in released)
            await seatMapUpdates.PublishAsync(eventId, seatIds, isAvailable: true);

        return new ReleaseExpiredHoldsResult(seatsReleased, orders.Count);
    }
}
