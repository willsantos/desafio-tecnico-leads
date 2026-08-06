namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Base for "action rejected because the lead isn't in the state this step requires" 409
/// exceptions (<see cref="ConfirmationInProgressException"/>, <see cref="LeadAlreadyCompletedException"/>,
/// <see cref="ConsultationNotCompletedException"/>, <see cref="NoPriorConfirmationAttemptException"/>).
/// Subclasses supply their own Portuguese-correct message; this only centralizes the shared
/// leadId-carrying shape so a new state-precondition case doesn't redefine an identical
/// constructor/property pair.
/// </summary>
public abstract class LeadStateConflictException(string leadId, string message) : Exception(message)
{
    public string LeadId { get; } = leadId;
}
