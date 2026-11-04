using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using WLVSSeatSync.Infrastructure.Persistence;

namespace WLVSSeatSync.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        // "seatsyncdb" must match AddDatabase("seatsyncdb") in the AppHost.
        builder.AddNpgsqlDbContext<SeatSyncDbContext>("seatsyncdb");

        // One shared clock. Tests can swap in a fake one later.
        builder.Services.AddSingleton(TimeProvider.System);

        return builder;
    }
}
