namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>Thrown when a document id does not match any active document. Maps to 404.</summary>
public class DocumentNotFoundException(string documentId) : EntityNotFoundException(documentId, $"Documento '{documentId}' não encontrado.")
{
    public string DocumentId => EntityId;
}
