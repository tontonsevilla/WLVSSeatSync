using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Application.Common;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Application.Events;

public sealed record GetSeatMapQuery(Guid EventId) : IRequest<Result<IReadOnlyList<SeatMapSeat>>>;

public sealed record SeatMapSeat(Guid EventSeatId, string Section, string Row, int Number,
    Guid TicketTierId, bool IsAvailable);

internal sealed class GetSeatMapQueryHandler(IApplicationDbContext db, 
    TimeProvider clock,
    HybridCache cache)
    : IRequestHandler<GetSeatMapQuery, Result<IReadOnlyList<SeatMapSeat>>>
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(30),
        LocalCacheExpiration = TimeSpan.FromSeconds(5),
    };

    public async Task<Result<IReadOnlyList<SeatMapSeat>>> Handle(GetSeatMapQuery request,
        CancellationToken cancellationToken)
    {
        var seats = await cache.GetOrCreateAsync(
            CacheKeys.SeatMap(request.EventId),
            async ct => await LoadAsync(request.EventId, ct),
            CacheOptions,
            tags: [CacheKeys.Event(request.EventId)],
            cancellationToken: cancellationToken);

        return seats is null
            ? EventErrors.NotFound(request.EventId)
            : Result.Success<IReadOnlyList<SeatMapSeat>>(seats);
    }

    private async Task<IReadOnlyList<SeatMapSeat>?> LoadAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var ev = await db.Events
            .AsNoTracking()
            .Include(e => e.Seats)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (ev is null)
            return null;
        var positions = await db.Venues
            .AsNoTracking()
            .Where(v => v.Id == ev.VenueId)
            .SelectMany(v => v.Seats)
            .ToDictionaryAsync(s => s.Id, s => s.Position, cancellationToken);
        var now = clock.GetUtcNow();
        var seats = ev.Seats
            .Select(s => (Seat: s, Position: positions[s.SeatId]))
            .OrderBy(x => x.Position.Section)
            .ThenBy(x => x.Position.Row)
            .ThenBy(x => x.Position.Number)
            .Select(x => new SeatMapSeat(x.Seat.Id, x.Position.Section, x.Position.Row, x.Position.Number,
                x.Seat.TicketTierId, x.Seat.IsAvailable(now)))
            .ToList();
        return seats;
    }
}
