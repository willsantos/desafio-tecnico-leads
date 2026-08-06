namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Thrown when <c>retry-submission</c> is called on a lead that never had a confirm attempt
/// (spec P1-6 AC13). Maps to 409.
/// </summary>
public class NoPriorConfirmationAttemptException(string leadId)
    : LeadStateConflictException(leadId, $"O lead '{leadId}' nunca teve uma tentativa de confirmação anterior.");
