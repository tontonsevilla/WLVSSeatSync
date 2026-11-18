using Microsoft.Extensions.Hosting;

using MongoDB.Driver;

namespace WLVSSeatSync.Infrastructure.Documents;

internal sealed class MongoIndexSetup(IMongoDatabase database) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var reviews = database.GetCollection<ReviewDocument>(MongoReviewStore.CollectionName);
        await reviews.Indexes.CreateManyAsync(
        [
            // One review per person per event, enforced by the database.
            new CreateIndexModel<ReviewDocument>(
                Builders<ReviewDocument>.IndexKeys.Ascending(r => r.EventId).Ascending(r => r.UserId),
                new CreateIndexOptions { Unique = true }),
            // "Newest reviews for this event".
            new CreateIndexModel<ReviewDocument>(
                Builders<ReviewDocument>.IndexKeys.Ascending(r => r.EventId).Descending(r => r.CreatedAtUtc)),
        ], cancellationToken);

        var audit = database.GetCollection<AuditDocument>(MongoAuditLog.CollectionName);
        await audit.Indexes.CreateManyAsync(
        [
            // A TTL index: MongoDB itself deletes entries once they're a year old.
            new CreateIndexModel<AuditDocument>(
                Builders<AuditDocument>.IndexKeys.Ascending(a => a.AtUtc),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(365) }),
            new CreateIndexModel<AuditDocument>(
                Builders<AuditDocument>.IndexKeys.Ascending(a => a.Action).Descending(a => a.AtUtc)),
        ], cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
