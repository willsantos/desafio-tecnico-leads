namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Thrown when a simulation id does not match any simulation on the lead (spec P1-2 AC8).
/// Distinct from <see cref="LeadNotFoundException"/> for the same "select" endpoint's two
/// different 404 causes. Maps to 404.
/// </summary>
public class SimulationNotFoundException(string simulationId)
    : Exception($"Simulação '{simulationId}' não encontrada.")
{
    public string SimulationId { get; } = simulationId;
}
