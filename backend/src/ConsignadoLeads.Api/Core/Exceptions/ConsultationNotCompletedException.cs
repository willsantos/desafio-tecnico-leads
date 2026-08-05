namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Thrown when a step that requires a completed consultation (etapa 1) is attempted before
/// it exists (spec P1-2 AC6). Not among design.md's original 8 exception types — added here
/// as its first consumer since no existing type maps to this 409 case. Maps to 409.
/// </summary>
public class ConsultationNotCompletedException(string leadId)
    : Exception($"A consulta de elegibilidade do lead '{leadId}' ainda não foi concluída.")
{
    public string LeadId { get; } = leadId;
}
