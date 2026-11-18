namespace WLVSSeatSync.Application.Abstractions;

// Marks a command whose successful runs are recorded in the audit log.
public interface IAuditable
{
}

// Who is making the current request. The API implements it from the JWT.
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
}

public sealed record AuditRecord(DateTimeOffset At, string Action, Guid? UserId, string? UserEmail,
    Guid? EntityId, object Details);

public sealed record AuditEntry(DateTimeOffset At, string Action, string? UserEmail, Guid? EntityId, string Details);

public interface IAuditLog
{
    Task WriteAsync(AuditRecord record, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEntry>> GetLatestAsync(int take, string? action, CancellationToken cancellationToken);
}
