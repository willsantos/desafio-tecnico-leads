namespace ConsignadoLeads.Api.Features.Simulation;

public record CreateSimulationRequest(decimal RequestedAmount, int Installments);

public record SimulationResponseDto(
    string Id,
    decimal RequestedAmount,
    int Installments,
    decimal InterestRate,
    decimal InstallmentAmount,
    decimal TotalAmount,
    bool Selected,
    DateTime SimulatedAt);

public static class SimulationResponseMapper
{
    public static SimulationResponseDto ToDto(Core.Models.Simulation simulation) => new(
        simulation.Id,
        simulation.RequestedAmount,
        simulation.Installments,
        simulation.InterestRate,
        simulation.InstallmentAmount,
        simulation.TotalAmount,
        simulation.Selected,
        simulation.SimulatedAt);
}
