using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Contracts;
using WLVSSeatSync.Domain.Orders;

namespace WLVSSeatSync.Application.Reminders;

public sealed record SendEventRemindersCommand : IRequest<int>;

internal sealed class SendEventRemindersCommandHandler(
    IApplicationDbContext db, IUserDirectory users, IEventPublisher events, TimeProvider clock)
    : IRequestHandler<SendEventRemindersCommand, int>
{
    public async Task<int> Handle(SendEventRemindersCommand request, CancellationToken cancellationToken)
    {
        // The job runs hourly; each run takes events starting 23 to 24 hours from now.
        var now = clock.GetUtcNow();
        var windowStart = now.AddHours(23);
        var windowEnd = now.AddHours(24);

        var due = await (
                from o in db.Orders.AsNoTracking()
                join e in db.Events on o.EventId equals e.Id
                join v in db.Venues on e.VenueId equals v.Id
                where o.Status == OrderStatus.Paid && e.StartsAt > windowStart && e.StartsAt <= windowEnd
                select new { o.Id, o.CustomerId, e.Title, e.StartsAt, VenueName = v.Name })
            .ToListAsync(cancellationToken);

        foreach (var order in due)
        {
            var customer = await users.FindAsync(order.CustomerId, cancellationToken);
            if (customer is null)
                continue;

            await events.PublishAsync(new EventReminderDue(order.Id, customer.Email, customer.DisplayName,
                order.Title, order.StartsAt, order.VenueName), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken); // commits the outbox messages
        return due.Count;
    }
}