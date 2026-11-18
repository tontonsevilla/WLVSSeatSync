using MediatR;

using Microsoft.Extensions.Logging;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Application.Behaviors;

internal sealed class AuditBehavior<TRequest, TResponse>(
    IAuditLog auditLog,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // Only marked commands, and only when they worked.
        if (request is not IAuditable || response is not Result { IsSuccess: true })
            return response;

        var action = typeof(TRequest).Name.Replace("Command", string.Empty, StringComparison.Ordinal);
        Guid? entityId = response is Result<Guid> created ? created.Value : null;

        try
        {
            var record = new AuditRecord(clock.GetUtcNow(), action, currentUser.UserId, currentUser.Email,
                entityId, request);
            await auditLog.WriteAsync(record, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // The change is already saved in PostgreSQL. Failing the request now would only confuse the user.
            logger.LogWarning(ex, "Could not write the audit entry for {Action}", action);
        }

        return response;
    }
}
