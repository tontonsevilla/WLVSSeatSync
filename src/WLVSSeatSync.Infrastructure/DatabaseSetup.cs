using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Infrastructure.Identity;
using WLVSSeatSync.Infrastructure.Persistence;
using WLVSSeatSync.Infrastructure.Persistence.Seed;

namespace WLVSSeatSync.Infrastructure;

public static class DatabaseSetup
{
    // Development only. Never ship demo accounts to production.
    public const string DemoPassword = "Passw0rd!";

    private static readonly (string Email, string DisplayName, string Role)[] DemoUsers =
    [
        ("admin@seatsync.dev", "Ana Admin", Roles.Admin),
        ("organizer@seatsync.dev", "Oscar Organizer", Roles.Organizer),
        ("staff@seatsync.dev", "Sam Staff", Roles.Staff),
        ("attendee@seatsync.dev", "Andrea Attendee", Roles.Attendee),
    ];

    public static async Task MigrateAndSeedDatabaseAsync(this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SeatSyncDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        await db.Database.MigrateAsync(cancellationToken);
        await DevDataSeeder.SeedAsync(db, clock.GetUtcNow(), cancellationToken);
        await SeedRolesAndDemoUsersAsync(scope.ServiceProvider);
    }

    private static async Task SeedRolesAndDemoUsersAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var (email, displayName, role) in DemoUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
            };

            var created = await userManager.CreateAsync(user, DemoPassword);
            if (!created.Succeeded)
                throw new InvalidOperationException(
                    $"Could not create {email}: {string.Join("; ", created.Errors.Select(e => e.Description))}");

            await userManager.AddToRoleAsync(user, role);
        }
    }
}
