using WLVSSeatSync.Domain.Abstractions;

namespace WLVSSeatSync.Application.Reviews;

public sealed record NewReview(Guid EventId, Guid UserId, string AuthorName, int Rating, string Comment,
    DateTimeOffset CreatedAt);

public sealed record ReviewItem(string AuthorName, int Rating, string Comment, DateTimeOffset CreatedAt);

public sealed record EventReviews(double? AverageRating, int Count, IReadOnlyList<ReviewItem> Latest);

public interface IReviewStore
{
    // Returns false when this person has already reviewed this event.
    Task<bool> AddAsync(NewReview review, CancellationToken cancellationToken);
    Task<EventReviews> GetForEventAsync(Guid eventId, int latest, CancellationToken cancellationToken);
}

public static class ReviewErrors
{
    public static readonly Error NoTicket =
        new("Review.NoTicket", "Only people with a ticket to this event can review it.");
    public static readonly Error AlreadyReviewed =
        Error.Conflict("Review.AlreadyReviewed", "You have already reviewed this event.");
}
