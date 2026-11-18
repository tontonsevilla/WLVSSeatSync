using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WLVSSeatSync.Infrastructure.Documents;

public sealed class ReviewDocument
{
    [BsonId, BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid EventId { get; set; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid UserId { get; set; }

    public string AuthorName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AuditDocument
{
    [BsonId, BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    public DateTime AtUtc { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? EntityId { get; set; }
    public BsonDocument Details { get; set; } = new(); // any shape: that's the point of a document store
}
