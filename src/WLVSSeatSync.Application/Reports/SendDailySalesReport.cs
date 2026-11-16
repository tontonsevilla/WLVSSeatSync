using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Contracts;
using WLVSSeatSync.Domain.Orders;

namespace WLVSSeatSync.Application.Reports;

public sealed record SendDailySalesReportCommand : IRequest<int>;

internal sealed class SendDailySalesReportCommandHandler(
    IApplicationDbContext db, IUserDirectory users, IEventPublisher events, TimeProvider clock)
    : IRequestHandler<SendDailySalesReportCommand, int>
{
    private static readonly TimeSpan PhilippineTime = TimeSpan.FromHours(8);

    public async Task<int> Handle(SendDailySalesReportCommand request, CancellationToken cancellationToken)
    {
        // "Yesterday" in Philippine time, converted to UTC for PostgreSQL.
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(PhilippineTime).DateTime);
        var day = today.AddDays(-1);
        var dayStart = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), PhilippineTime).ToUniversalTime();
        var dayEnd = dayStart.AddDays(1);

        var sold = await (
                from o in db.Orders.AsNoTracking()
                from item in o.Items
                join e in db.Events on o.EventId equals e.Id
                where o.Status == OrderStatus.Paid && o.PaidAt >= dayStart && o.PaidAt < dayEnd
                select new { e.Title, item.Price.Amount, item.Price.Currency })
            .ToListAsync(cancellationToken);

        var byEvent = sold
            .GroupBy(s => s.Title)
            .Select(g => new EventSales(g.Key, g.Count(), g.Sum(s => s.Amount), g.First().Currency))
            .OrderByDescending(e => e.Revenue)
            .ToList();

        var recipients = await users.GetEmailsInRoleAsync(Roles.Admin, cancellationToken);
        if (recipients.Count == 0)
            return 0;

        await events.PublishAsync(new DailySalesReportReady(day, recipients, byEvent), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return byEvent.Count;
    }
}