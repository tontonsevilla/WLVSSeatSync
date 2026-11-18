using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Application.Events;

public sealed record GetManagedEventsQuery : IRequest<IReadOnlyList<ManagedEvent>>;

public sealed record ManagedEvent(Guid Id, string Title, DateTimeOffset StartsAt, string Status,
    string VenueName, int TotalSeats, int SeatsSold);

internal sealed class GetManagedEventsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetManagedEventsQuery, IReadOnlyList<ManagedEvent>>
{
    public async Task<IReadOnlyList<ManagedEvent>> Handle(GetManagedEventsQuery request,
        CancellationToken cancellationToken)
    {
        // Drafts and past events included: this list is for the people running them.
        var rows = await (
                from e in db.Events.AsNoTracking()
                join v in db.Venues on e.VenueId equals v.Id
                orderby e.StartsAt descending
                select new
                {
                    e.Id,
                    e.Title,
                    e.StartsAt,
                    e.Status,
                    VenueName = v.Name,
                    TotalSeats = e.Seats.Count(),
                    SeatsSold = e.Seats.Count(s => s.Status == SeatStatus.Sold),
                })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(r => new ManagedEvent(r.Id, r.Title, r.StartsAt, r.Status.ToString(),
            r.VenueName, r.TotalSeats, r.SeatsSold))];
    }
}
