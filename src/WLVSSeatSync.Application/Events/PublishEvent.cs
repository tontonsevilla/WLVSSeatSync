using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Application.Common;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Application.Events;

public sealed record PublishEventCommand(Guid EventId) : IRequest<Result>;

internal sealed class PublishEventCommandValidator : AbstractValidator<PublishEventCommand>
{
    public PublishEventCommandValidator() => RuleFor(c => c.EventId).NotEmpty();
}

internal sealed class PublishEventCommandHandler(IApplicationDbContext db,
    HybridCache cache)
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

        await cache.RemoveByTagAsync([CacheKeys.AllEvents, CacheKeys.Event(request.EventId)], cancellationToken);

        return Result.Success();
    }
}
