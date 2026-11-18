using System.Text.Json;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

using WLVSSeatSync.Application.Abstractions;

namespace WLVSSeatSync.Infrastructure.Documents;

internal sealed class MongoAuditLog(IMongoDatabase database) : IAuditLog
{
    public const string CollectionName = "audit_log";

    private static readonly JsonWriterSettings PrettyJson = new() { Indent = true };

    private IMongoCollection<AuditDocument> Entries => database.GetCollection<AuditDocument>(CollectionName);

    public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        // Serialize the command as the API would, then store it as a real document, not a string,
        // so you can query inside it, such as "every event created at venue X".
        var json = JsonSerializer.Serialize(record.Details, record.Details.GetType(), JsonSerializerOptions.Web);

        return Entries.InsertOneAsync(new AuditDocument
        {
            Id = Guid.CreateVersion7(),
            AtUtc = record.At.UtcDateTime,
            Action = record.Action,
            UserId = record.UserId?.ToString(),
            UserEmail = record.UserEmail,
            EntityId = record.EntityId?.ToString(),
            Details = BsonDocument.Parse(json),
        }, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetLatestAsync(int take, string? action,
        CancellationToken cancellationToken)
    {
        var filter = string.IsNullOrWhiteSpace(action)
            ? Builders<AuditDocument>.Filter.Empty
            : Builders<AuditDocument>.Filter.Eq(a => a.Action, action);

        var documents = await Entries.Find(filter)
            .SortByDescending(a => a.AtUtc)
            .Limit(take)
            .ToListAsync(cancellationToken);

        return [.. documents.Select(d => new AuditEntry(
            new DateTimeOffset(d.AtUtc, TimeSpan.Zero),
            d.Action,
            d.UserEmail,
            Guid.TryParse(d.EntityId, out var id) ? id : null,
            d.Details.ToJson(PrettyJson)))];
    }
}
