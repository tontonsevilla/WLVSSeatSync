using MediatR;

namespace WLVSSeatSync.Application.Reviews;

public sealed record GetEventReviewsQuery(Guid EventId) : IRequest<EventReviews>;

internal sealed class GetEventReviewsQueryHandler(IReviewStore reviews)
    : IRequestHandler<GetEventReviewsQuery, EventReviews>
{
    public Task<EventReviews> Handle(GetEventReviewsQuery request, CancellationToken cancellationToken) =>
        reviews.GetForEventAsync(request.EventId, latest: 20, cancellationToken);
}
