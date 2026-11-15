using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using WLVSSeatSync.Application.Abstractions;
using WLVSSeatSync.Application.Common;
using WLVSSeatSync.Domain.Common;
using WLVSSeatSync.Domain.Events;

namespace WLVSSeatSync.Application.Events;

public sealed record SearchEventsQuery(string? Search, DateTimeOffset? From, int Page, int PageSize)
    : IRequest<PagedList<EventSummary>>;

public sealed record EventSummary(Guid Id, string Title, DateTimeOffset StartsAt,
    string VenueName, string City, decimal LowestPrice, string Currency);

internal sealed class SearchEventsQueryValidator : AbstractValidator<SearchEventsQuery>
{
    public SearchEventsQueryValidator()
    {
        RuleFor(q => q.Search).MaximumLength(100);
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

internal sealed class SearchEventsQueryHandler(IApplicationDbContext db, 
    TimeProvider clock,
    HybridCache cache)
    : IRequestHandler<SearchEventsQuery, PagedList<EventSummary>>
{
    public async Task<PagedList<EventSummary>> Handle(SearchEventsQuery request, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            CacheKeys.Search(request.Search, request.From, request.Page, request.PageSize),
            async ct => await QueryAsync(request, ct),
            tags: [CacheKeys.AllEvents],
            cancellationToken: cancellationToken);

    private async Task<PagedList<EventSummary>> QueryAsync(SearchEventsQuery request, CancellationToken cancellationToken)
    {
        var from = (request.From ?? clock.GetUtcNow()).ToUniversalTime();

        var events = db.Events
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published && e.StartsAt >= from);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            events = events.Where(e => e.Title.ToLower().Contains(term));
        }

        var totalCount = await events.CountAsync(cancellationToken);

        var items = await (
                from e in events
                join v in db.Venues on e.VenueId equals v.Id
                orderby e.StartsAt
                select new EventSummary(
                    e.Id, e.Title, e.StartsAt, v.Name, v.City,
                    e.TicketTiers.Min(t => (decimal?)t.Price.Amount) ?? 0,
                    e.TicketTiers.Select(t => t.Price.Currency).FirstOrDefault() ?? Money.Php))
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<EventSummary>(items, request.Page, request.PageSize, totalCount);
    }
}
