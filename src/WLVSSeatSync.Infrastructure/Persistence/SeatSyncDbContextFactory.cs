using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WLVSSeatSync.Infrastructure.Persistence;

// Used only by "dotnet ef" commands. Creating a migration never connects to this database.
public sealed class SeatSyncDbContextFactory : IDesignTimeDbContextFactory<SeatSyncDbContext>
{
    public SeatSyncDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SeatSyncDbContext>()
            .UseNpgsql("Host=localhost;Database=seatsync_design;Username=postgres;Password=postgres")
            .Options;

        return new SeatSyncDbContext(options);
    }
}
