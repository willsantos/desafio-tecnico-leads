namespace ConsignadoLeads.Api.Features.Identification;

/// <summary>Mocked identification check (README seção 4.3). Deterministic, no external call.</summary>
public static class IdentificationMock
{
    public static string Check(IdentificationOutcome outcome) => outcome.ToString();
}
