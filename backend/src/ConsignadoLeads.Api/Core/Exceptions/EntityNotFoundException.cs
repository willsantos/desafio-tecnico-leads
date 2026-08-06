namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Base for "entity referenced by id doesn't exist" 404 exceptions (<see cref="LeadNotFoundException"/>,
/// <see cref="DocumentNotFoundException"/>, <see cref="SimulationNotFoundException"/>). Subclasses
/// supply their own Portuguese-correct message; this only centralizes the shared id-carrying shape
/// so a new not-found case doesn't redefine an identical constructor/property pair.
/// </summary>
public abstract class EntityNotFoundException(string entityId, string message) : Exception(message)
{
    public string EntityId { get; } = entityId;
}
