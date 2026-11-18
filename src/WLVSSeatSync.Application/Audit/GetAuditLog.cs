using FluentValidation;

using MediatR;

using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Application.Audit;

public sealed record GetAuditLogQuery(string? Action, int Take) : IRequest<IReadOnlyList<AuditEntry>>;

internal sealed class GetAuditLogQueryValidator : AbstractValidator<GetAuditLogQuery>
{
    public GetAuditLogQueryValidator()
    {
        RuleFor(q => q.Take).InclusiveBetween(1, 500);
        RuleFor(q => q.Action).MaximumLength(100);
    }
}

internal sealed class GetAuditLogQueryHandler(IAuditLog auditLog)
    : IRequestHandler<GetAuditLogQuery, IReadOnlyList<AuditEntry>>
{
    public Task<IReadOnlyList<AuditEntry>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken) =>
        auditLog.GetLatestAsync(request.Take, request.Action, cancellationToken);
}
