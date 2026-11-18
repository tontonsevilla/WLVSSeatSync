using MediatR;

using Microsoft.EntityFrameworkCore;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Domain.Abstractions;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Application.Analytics;

public sealed record GetEventSalesQuery(Guid EventId) : IRequest<Result<EventSalesReport>>;

internal sealed class GetEventSalesQueryHandler(IApplicationDbContext db, ISalesAnalytics analytics)
    : IRequestHandler<GetEventSalesQuery, Result<EventSalesReport>>
{
    // A sale at 1 a.m. in Manila belongs to that Manila day, not to the previous UTC day.
    private const string ReportTimeZone = "Asia/Manila";

    public async Task<Result<EventSalesReport>> Handle(GetEventSalesQuery request, CancellationToken cancellationToken)
    {
        var title = await db.Events
            .AsNoTracking()
            .Where(e => e.Id == request.EventId)
            .Select(e => e.Title)
            .FirstOrDefaultAsync(cancellationToken);

        if (title is null)
            return EventErrors.NotFound(request.EventId);

        var tiers = await analytics.GetTierSalesAsync(request.EventId, cancellationToken);
        var days = await analytics.GetDailySalesAsync(request.EventId, ReportTimeZone, cancellationToken);

        return new EventSalesReport(request.EventId, title, tiers, days);
    }
}
