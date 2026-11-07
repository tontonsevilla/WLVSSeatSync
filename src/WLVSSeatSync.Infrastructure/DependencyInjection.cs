using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Infrastructure.Identity;
using WLVSSeatSync.Infrastructure.Persistence;

namespace WLVSSeatSync.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<SeatSyncDbContext>("seatsyncdb");
        builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SeatSyncDbContext>());
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SeatSyncDbContext>();

        return builder;
    }
}
