namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Thrown when etapa 6 confirmation is blocked by unmet requirements (missing simulation,
/// incomplete identification, incompatible document, missing required documents), or when
/// the mocked main system returns <c>rejected</c>/<c>validationError</c>. Maps to 422 with
/// the exact list of reasons.
/// </summary>
public class PendingRequirementsException(IReadOnlyList<string> reasons)
    : Exception("Existem pendências que impedem a confirmação.")
{
    public IReadOnlyList<string> Reasons { get; } = reasons;
}
