namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>Thrown when a lead id does not match any document. Maps to 404.</summary>
public class LeadNotFoundException(string leadId) : EntityNotFoundException(leadId, $"Lead '{leadId}' não encontrado.")
{
    public string LeadId => EntityId;
}
