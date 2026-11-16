namespace WLVSSeatSync.Application.Abstractions;

public sealed record UserContact(string Email, string DisplayName);

public interface IUserDirectory
{
    Task<UserContact?> FindAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetEmailsInRoleAsync(string role, CancellationToken cancellationToken);
}
