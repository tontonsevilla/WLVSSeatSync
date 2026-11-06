using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Application.Events;

public sealed record GetSeatMapQuery(Guid EventId) : IRequest<Result<IReadOnlyList<SeatMapSeat>>>;

public sealed record SeatMapSeat(Guid EventSeatId, string Section, string Row, int Number,
    Guid TicketTierId, bool IsAvailable);

internal sealed class GetSeatMapQueryHandler(IApplicationDbContext db, TimeProvider clock)
    : IRequestHandler<GetSeatMapQuery, Result<IReadOnlyList<SeatMapSeat>>>
{
    public async Task<Result<IReadOnlyList<SeatMapSeat>>> Handle(GetSeatMapQuery request,
        CancellationToken cancellationToken)
    {
        var ev = await db.Events
            .AsNoTracking()
            .Include(e => e.Seats)
            .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

        if (ev is null)
            return EventErrors.NotFound(request.EventId);

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

        return Result.Success<IReadOnlyList<SeatMapSeat>>(seats);
    }
}
