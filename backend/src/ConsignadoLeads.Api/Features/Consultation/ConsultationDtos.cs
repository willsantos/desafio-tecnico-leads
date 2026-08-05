namespace ConsignadoLeads.Api.Features.Consultation;

/// <summary>
/// Request body for both <c>POST /leads/consultation</c> and <c>PUT /leads/{id}/steps/consultation</c>.
/// Fields are nullable so a missing required field can be distinguished from an empty one for
/// the 400 validation branch (spec P1-1 AC2).
/// </summary>
public record ConsultationRequest(
    string? Cpf,
    string? BirthDate,
    string? BenefitType,
    string? BenefitNumber,
    string? PayingInstitution,
    bool? ConsultationAuthorized,
    string? MockOutcome,
    int? ExpectedVersion);

/// <summary>Values accepted by the mocked eligibility check (README seção 5 mockOutcome table).</summary>
public enum EligibilityOutcome
{
    eligible,
    not_eligible,
    unavailable,
}
