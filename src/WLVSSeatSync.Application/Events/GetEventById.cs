using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Application.Common;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Application.Events;

public sealed record GetEventByIdQuery(Guid EventId) : IRequest<Result<EventDetails>>;

public sealed record EventDetails(Guid Id, string Title, string Description, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, string Status, VenueInfo Venue, IReadOnlyList<TicketTierDetails> TicketTiers);

public sealed record VenueInfo(Guid Id, string Name, string City);

public sealed record TicketTierDetails(Guid Id, string Name, decimal Price, string Currency,
    int AvailableSeats, int TotalSeats);

internal sealed class GetEventByIdQueryHandler(IApplicationDbContext db, 
    TimeProvider clock,
    HybridCache cache)
    : IRequestHandler<GetEventByIdQuery, Result<EventDetails>>
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(30),
        LocalCacheExpiration = TimeSpan.FromSeconds(5),
    };

    public async Task<Result<EventDetails>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var details = await cache.GetOrCreateAsync(
            CacheKeys.EventDetails(request.EventId),
            async ct => await LoadAsync(request.EventId, ct),
            CacheOptions,
            tags: [CacheKeys.Event(request.EventId)],
            cancellationToken: cancellationToken);

        return details is null ? EventErrors.NotFound(request.EventId) : details;
    }

    private async Task<EventDetails?> LoadAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var ev = await db.Events
            .AsNoTracking()
            .Include(e => e.TicketTiers)
            .Include(e => e.Seats)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (ev is null)
            return null;

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
