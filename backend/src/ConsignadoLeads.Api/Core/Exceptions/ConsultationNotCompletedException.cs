namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Thrown when a step that requires a completed consultation (etapa 1) is attempted before
/// it exists (spec P1-2 AC6). Maps to 409.
/// </summary>
public class ConsultationNotCompletedException(string leadId)
    : LeadStateConflictException(leadId, $"A consulta de elegibilidade do lead '{leadId}' ainda não foi concluída.");
