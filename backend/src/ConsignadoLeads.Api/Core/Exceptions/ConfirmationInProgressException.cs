namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Thrown when a confirm/retry-submission mutex is already held for a lead. Maps to 409.
/// </summary>
public class ConfirmationInProgressException(string leadId)
    : Exception($"Já existe uma confirmação em andamento para o lead '{leadId}'.")
{
    public string LeadId { get; } = leadId;
}
