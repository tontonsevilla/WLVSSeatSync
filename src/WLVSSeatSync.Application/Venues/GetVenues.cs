using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Application.Venues;

public sealed record GetVenuesQuery : IRequest<IReadOnlyList<VenueSummary>>;

public sealed record VenueSummary(Guid Id, string Name, string City, int SeatCount);

internal sealed class GetVenuesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVenuesQuery, IReadOnlyList<VenueSummary>>
{
    public async Task<IReadOnlyList<VenueSummary>> Handle(GetVenuesQuery request, CancellationToken cancellationToken) =>
        await db.Venues
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .Select(v => new VenueSummary(v.Id, v.Name, v.City, v.Seats.Count()))
            .ToListAsync(cancellationToken);
}
