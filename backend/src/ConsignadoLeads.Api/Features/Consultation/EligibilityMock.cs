namespace ConsignadoLeads.Api.Features.Consultation;

/// <summary>Mocked eligibility check (README seção 4.1). Deterministic, no external call.</summary>
public static class EligibilityMock
{
    public static decimal? AvailableMargin(EligibilityOutcome outcome) => outcome switch
    {
        EligibilityOutcome.eligible => 350.00m,
        _ => null,
    };
}
