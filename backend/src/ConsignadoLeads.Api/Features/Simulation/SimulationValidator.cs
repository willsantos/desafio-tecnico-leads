namespace ConsignadoLeads.Api.Features.Simulation;

/// <summary>
/// Input validation for <see cref="CreateSimulationRequest"/>, shared by the create endpoint.
/// Pure function — no Mongo access.
/// </summary>
public static class SimulationValidator
{
    public static IReadOnlyList<string> Validate(CreateSimulationRequest request)
    {
        var errors = new List<string>();

        if (request.RequestedAmount <= 0)
        {
            errors.Add("requestedAmount deve ser maior que zero.");
        }

        if (request.Installments <= 0)
        {
            errors.Add("installments deve ser maior que zero.");
        }

        return errors;
    }
}
