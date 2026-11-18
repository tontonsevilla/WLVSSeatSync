using MongoDB.Driver;

using WLVSSeatSync.Application.Reviews;

namespace WLVSSeatSync.Infrastructure.Documents;

internal sealed class MongoReviewStore(IMongoDatabase database) : IReviewStore
{
    public const string CollectionName = "reviews";

    private IMongoCollection<ReviewDocument> Reviews => database.GetCollection<ReviewDocument>(CollectionName);

    public async Task<bool> AddAsync(NewReview review, CancellationToken cancellationToken)
    {
        try
        {
            await Reviews.InsertOneAsync(new ReviewDocument
            {
                Id = Guid.CreateVersion7(),
                EventId = review.EventId,
                UserId = review.UserId,
                AuthorName = review.AuthorName,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAtUtc = review.CreatedAt.UtcDateTime,
            }, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false; // the unique index on (EventId, UserId) said no
        }
    }

    public async Task<EventReviews> GetForEventAsync(Guid eventId, int latest, CancellationToken cancellationToken)
    {
        // An aggregation pipeline: MongoDB's GROUP BY. It runs on the server.
        var stats = await Reviews.Aggregate()
            .Match(r => r.EventId == eventId)
            .Group(r => r.EventId, g => new { Count = g.Count(), Average = g.Average(r => r.Rating) })
            .FirstOrDefaultAsync(cancellationToken);

        var newest = await Reviews.Find(r => r.EventId == eventId)
            .SortByDescending(r => r.CreatedAtUtc)
            .Limit(latest)
            .ToListAsync(cancellationToken);

        var items = newest
            .Select(r => new ReviewItem(r.AuthorName, r.Rating, r.Comment, new DateTimeOffset(r.CreatedAtUtc, TimeSpan.Zero)))
            .ToList();

        return new EventReviews(stats?.Average, stats?.Count ?? 0, items);
    }
}
