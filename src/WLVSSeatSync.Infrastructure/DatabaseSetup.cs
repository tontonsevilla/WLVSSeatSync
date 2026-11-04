using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WLVSSeatSync.Infrastructure.Persistence;
using WLVSSeatSync.Infrastructure.Persistence.Seed;

namespace WLVSSeatSync.Infrastructure;

public static class DatabaseSetup
{
    // Development only: apply any pending migrations, then add sample data.
    public static async Task MigrateAndSeedDatabaseAsync(this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SeatSyncDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        await db.Database.MigrateAsync(cancellationToken);
        await DevDataSeeder.SeedAsync(db, clock.GetUtcNow(), cancellationToken);
    }
}
