using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Application.Events;

public sealed record GetEventByIdQuery(Guid EventId) : IRequest<Result<EventDetails>>;

public sealed record EventDetails(Guid Id, string Title, string Description, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, string Status, VenueInfo Venue, IReadOnlyList<TicketTierDetails> TicketTiers);

public sealed record VenueInfo(Guid Id, string Name, string City);

public sealed record TicketTierDetails(Guid Id, string Name, decimal Price, string Currency,
    int AvailableSeats, int TotalSeats);

internal sealed class GetEventByIdQueryHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<GetEventByIdQuery, Result<EventDetails>>
{
    public async Task<Result<EventDetails>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var ev = await db.Events
            .AsNoTracking()
            .Include(e => e.TicketTiers)
            .Include(e => e.Seats)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (ev is null)
            return EventErrors.NotFound(request.EventId);

        var venue = await db.Venues
            .AsNoTracking()
            .Where(v => v.Id == ev.VenueId)
            .Select(v => new VenueInfo(v.Id, v.Name, v.City))
            .FirstAsync(cancellationToken);

        var now = clock.GetUtcNow();
        var tiers = ev.TicketTiers
            .Select(t => new TicketTierDetails(t.Id, t.Name, t.Price.Amount, t.Price.Currency,
                AvailableSeats: ev.Seats.Count(s => s.TicketTierId == t.Id && s.IsAvailable(now)),
                TotalSeats: ev.Seats.Count(s => s.TicketTierId == t.Id)))
            .OrderByDescending(t => t.Price)
            .ToList();

        return new EventDetails(ev.Id, ev.Title, ev.Description, ev.StartsAt, ev.EndsAt,
            ev.Status.ToString(), venue, tiers);
    }
}
