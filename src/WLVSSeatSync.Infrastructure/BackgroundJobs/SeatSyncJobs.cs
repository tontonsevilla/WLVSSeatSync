using Hangfire;

using MediatR;

using WLVSSeatSync.Application.Orders;
using WLVSSeatSync.Application.Reminders;
using WLVSSeatSync.Application.Reports;

namespace WLVSSeatSync.Infrastructure.BackgroundJobs;

// Each job is a thin shell: Hangfire decides WHEN, a MediatR command does the work.

[DisableConcurrentExecution(timeoutInSeconds: 60)] // never two runs at once
[AutomaticRetry(Attempts = 0)]                     // the next run is only a minute away anyway
public sealed class ReleaseExpiredHoldsJob(ISender sender)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        sender.Send(new ReleaseExpiredHoldsCommand(), cancellationToken);
}

public sealed class EventRemindersJob(ISender sender)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        sender.Send(new SendEventRemindersCommand(), cancellationToken);
}

public sealed class DailySalesReportJob(ISender sender)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        sender.Send(new SendDailySalesReportCommand(), cancellationToken);
}

public static class SeatSyncJobs
{
    public static void Schedule(IRecurringJobManager jobs)
    {
        var manila = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

        // Hangfire swaps CancellationToken.None for the real token when the job runs.
        jobs.AddOrUpdate<ReleaseExpiredHoldsJob>("release-expired-holds",
            job => job.RunAsync(CancellationToken.None), Cron.Minutely());

        jobs.AddOrUpdate<EventRemindersJob>("event-reminders",
            job => job.RunAsync(CancellationToken.None), Cron.Hourly());

        jobs.AddOrUpdate<DailySalesReportJob>("daily-sales-report",
            job => job.RunAsync(CancellationToken.None), Cron.Daily(6),
            new RecurringJobOptions { TimeZone = manila });
    }
}