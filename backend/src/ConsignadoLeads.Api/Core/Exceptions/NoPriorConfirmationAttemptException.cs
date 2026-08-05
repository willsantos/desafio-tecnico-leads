namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Thrown when <c>retry-submission</c> is called on a lead that never had a confirm attempt
/// (spec P1-6 AC13). Not among design.md's original 8 exception types — added here as its
/// first consumer since no existing type maps to this 409 case. Maps to 409.
/// </summary>
public class NoPriorConfirmationAttemptException(string leadId)
    : Exception($"O lead '{leadId}' nunca teve uma tentativa de confirmação anterior.")
{
    public string LeadId { get; } = leadId;
}
