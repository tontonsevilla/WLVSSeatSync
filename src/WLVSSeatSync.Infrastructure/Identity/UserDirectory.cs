using Microsoft.AspNetCore.Identity;

using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Infrastructure.Identity;

internal sealed class UserDirectory(UserManager<ApplicationUser> users) : IUserDirectory
{
    public async Task<UserContact?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        return user?.Email is null ? null : new UserContact(user.Email, user.DisplayName);
    }

    public async Task<IReadOnlyList<string>> GetEmailsInRoleAsync(string role, CancellationToken cancellationToken)
    {
        var members = await users.GetUsersInRoleAsync(role);
        return [.. members.Select(u => u.Email).OfType<string>()];
    }
}
