using ConsignadoLeads.Api.Core.Dtos;

namespace ConsignadoLeads.Api.Features.Leads;

public record LeadsQuery(string? Status, string? CurrentStep, string? Cpf, int? Page, int? PageSize);

/// <summary>Summary shape for GET /leads (README seção 5): only the listed fields, nested as documented.
/// Cpf is an additive, non-breaking extension for the list view UI.</summary>
public record LeadSummaryDto(
    string Id,
    string Status,
    string Cpf,
    LeadSummaryProgressDto Progress,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    LeadSummaryFinalRegistrationDto? FinalRegistration);

public record LeadSummaryProgressDto(string CurrentStep);

public record LeadSummaryFinalRegistrationDto(string RegistrationId);

public record PagedLeadsResponse(IReadOnlyList<LeadSummaryDto> Items, int Page, int PageSize, long TotalItems, int TotalPages);

public static class LeadSummaryMapper
{
    public static LeadSummaryDto ToDto(Core.Models.Lead lead) => new(
        lead.Id,
        lead.Status,
        lead.Consultation?.Input.Cpf ?? string.Empty,
        new LeadSummaryProgressDto(lead.Progress.CurrentStep),
        lead.CreatedAt,
        lead.UpdatedAt,
        lead.Confirmation.FinalRegistration is null ? null : new LeadSummaryFinalRegistrationDto(lead.Confirmation.FinalRegistration.RegistrationId));
}
