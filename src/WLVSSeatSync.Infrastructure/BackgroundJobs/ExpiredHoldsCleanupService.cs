using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using WLVSSeatSync.Application.Orders;

namespace WLVSSeatSync.Infrastructure.BackgroundJobs;

internal sealed class ExpiredHoldsCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<ExpiredHoldsCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, clock);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // A hosted service lives for the whole app, but a DbContext must live for one unit
                // of work. So each run creates its own scope, and with it a fresh DbContext.
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new ReleaseExpiredHoldsCommand(), stoppingToken);

                if (result.SeatsReleased > 0 || result.OrdersExpired > 0)
                {
                    logger.LogInformation("Released {Seats} expired seat holds and expired {Orders} orders",
                        result.SeatsReleased, result.OrdersExpired);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One bad run must never stop the job; the next tick tries again.
                logger.LogError(ex, "Releasing expired holds failed; retrying in {Interval}", Interval);
            }
        }
    }
}
