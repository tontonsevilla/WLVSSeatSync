using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Application.Events;

public sealed record PublishEventCommand(Guid EventId) : IRequest<Result>;

internal sealed class PublishEventCommandValidator : AbstractValidator<PublishEventCommand>
{
    public PublishEventCommandValidator() => RuleFor(c => c.EventId).NotEmpty();
}

internal sealed class PublishEventCommandHandler(IApplicationDbContext db)
    : IRequestHandler<PublishEventCommand, Result>
{
    public async Task<Result> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await db.Events
            .Include(e => e.Seats)   // Publish() checks the event has seats
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (ev is null)
            return EventErrors.NotFound(request.EventId);

        var result = ev.Publish();
        if (result.IsFailure)
            return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
