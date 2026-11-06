using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Venues;

namespace WLVSSeatSync.Application.Venues;

public sealed record GetVenueByIdQuery(Guid VenueId) : IRequest<Result<VenueDetails>>;

public sealed record VenueDetails(Guid Id, string Name, string City, IReadOnlyList<VenueSection> Sections);

public sealed record VenueSection(string Name, int Rows, int Seats);

internal sealed class GetVenueByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVenueByIdQuery, Result<VenueDetails>>
{
    public async Task<Result<VenueDetails>> Handle(GetVenueByIdQuery request, CancellationToken cancellationToken)
    {
        var venue = await db.Venues
            .AsNoTracking()
            .Include(v => v.Seats)
            .FirstOrDefaultAsync(v => v.Id == request.VenueId, cancellationToken);

        if (venue is null)
            return VenueErrors.NotFound(request.VenueId);

        var sections = venue.Seats
            .GroupBy(s => s.Position.Section)
            .Select(g => new VenueSection(g.Key, g.Select(s => s.Position.Row).Distinct().Count(), g.Count()))
            .OrderBy(s => s.Name)
            .ToList();

        return new VenueDetails(venue.Id, venue.Name, venue.City, sections);
    }
}
