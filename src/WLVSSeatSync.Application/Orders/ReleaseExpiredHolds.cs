using System;
using System.Collections.Generic;
using System.Text;

using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Orders;

namespace WLVSSeatSync.Application.Orders;

public sealed record ReleaseExpiredHoldsCommand : IRequest<ReleaseExpiredHoldsResult>;

public sealed record ReleaseExpiredHoldsResult(int SeatsReleased, int OrdersExpired);

internal sealed class ReleaseExpiredHoldsCommandHandler(IApplicationDbContext db, TimeProvider clock)
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
        foreach (var ev in events)
            seatsReleased += ev.ReleaseExpiredHolds(now);

        var orders = await db.Orders
            .Where(o => o.Status == OrderStatus.Pending && o.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
            order.Expire(now);

        await db.SaveChangesAsync(cancellationToken);
        return new ReleaseExpiredHoldsResult(seatsReleased, orders.Count);
    }
}
