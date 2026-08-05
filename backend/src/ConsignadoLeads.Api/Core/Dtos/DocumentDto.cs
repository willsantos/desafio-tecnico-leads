using ConsignadoLeads.Api.Core.Models;

namespace ConsignadoLeads.Api.Core.Dtos;

/// <summary>
/// API-boundary representation of a `lead_documents` entry. Shared between the Documents
/// slice (which owns it) and the Leads slice (GET /leads/{id} joins active documents).
/// </summary>
public record DocumentDto(
    string Id,
    string LeadId,
    string Type,
    string? PersonalDocumentSubtype,
    string Status,
    DateTime UploadedAt);

public static class DocumentMapper
{
    public static DocumentDto ToDto(LeadDocumentEntity entity) => new(
        entity.Id,
        entity.LeadId,
        entity.Type,
        entity.PersonalDocumentSubtype,
        entity.Status,
        entity.UploadedAt);
}
