namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>Thrown when confirm/retry-submission is called on a lead already "completed". Maps to 409.</summary>
public class LeadAlreadyCompletedException(string leadId)
    : Exception($"O lead '{leadId}' já está completo.")
{
    public string LeadId { get; } = leadId;
}
