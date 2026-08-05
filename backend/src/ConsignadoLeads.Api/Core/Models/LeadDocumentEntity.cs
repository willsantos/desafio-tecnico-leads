using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ConsignadoLeads.Api.Core.Models;

/// <summary>
/// Persistence POCO for the "lead_documents" collection. References the uploaded file's
/// bytes via <see cref="GridFsFileId"/> (GridFS fs.files/fs.chunks), never returned directly
/// by an endpoint — API boundaries expose DTOs mapped from this type (spec P1-8 AC6).
/// </summary>
public class LeadDocumentEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    public string LeadId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    [BsonIgnoreIfNull]
    public string? PersonalDocumentSubtype { get; set; }

    public string Status { get; set; } = string.Empty;

    [BsonIgnoreIfNull]
    public string? Replaces { get; set; }

    public ObjectId GridFsFileId { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime UploadedAt { get; set; }
}
