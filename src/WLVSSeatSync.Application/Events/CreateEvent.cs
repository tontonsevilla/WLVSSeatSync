using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Common;
using WLVSSeatSync.Domain.Events;
using WLVSSeatSync.Domain.Venues;

namespace WLVSSeatSync.Application.Events;

public sealed record CreateEventCommand(
    Guid VenueId,
    string Title,
    string Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    IReadOnlyList<CreateTicketTier> Tiers) : IRequest<Result<Guid>>;

// One ticket tier, sold for every seat in one venue section.
public sealed record CreateTicketTier(string Name, string Section, decimal Price);

internal sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(c => c.VenueId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotNull().MaximumLength(4000);
        RuleFor(c => c.EndsAt).GreaterThan(c => c.StartsAt);
        RuleFor(c => c.Tiers).NotEmpty();
        RuleForEach(c => c.Tiers).ChildRules(tier =>
        {
            tier.RuleFor(t => t.Name).NotEmpty().MaximumLength(100);
            tier.RuleFor(t => t.Section).NotEmpty();
            tier.RuleFor(t => t.Price).GreaterThan(0).LessThan(1_000_000);
        });
    }
}

internal sealed class CreateEventCommandHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<CreateEventCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // 1. Load what the use case needs.
        var venue = await db.Venues
            .AsNoTracking()
            .Include(v => v.Seats)
            .FirstOrDefaultAsync(v => v.Id == request.VenueId, cancellationToken);

        if (venue is null)
            return VenueErrors.NotFound(request.VenueId);

        // 2. Ask the domain. 3. Stop at the first "no".
        var created = Event.Create(request.Title, request.Description, venue.Id,
            request.StartsAt.ToUniversalTime(), request.EndsAt.ToUniversalTime(), clock.GetUtcNow());
        if (created.IsFailure)
            return created.Error;

        var ev = created.Value;
        foreach (var tierRequest in request.Tiers)
        {
            var seatIds = venue.Seats
                .Where(s => s.Position.Section == tierRequest.Section)
                .Select(s => s.Id)
                .ToList();
            if (seatIds.Count == 0)
                return VenueErrors.SectionNotFound(tierRequest.Section);

            var tier = ev.AddTicketTier(tierRequest.Name, Money.Pesos(tierRequest.Price));
            if (tier.IsFailure)
                return tier.Error;

            var assigned = ev.AssignSeats(tier.Value.Id, seatIds);
            if (assigned.IsFailure)
                return assigned.Error;
        }

        // 4. Save.
        db.Events.Add(ev);
        await db.SaveChangesAsync(cancellationToken);
        return ev.Id;
    }
}
