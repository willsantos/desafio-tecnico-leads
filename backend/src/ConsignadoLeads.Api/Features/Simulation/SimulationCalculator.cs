namespace ConsignadoLeads.Api.Features.Simulation;

/// <summary>
/// Pure Price-formula calculator (README seção 4.2). No Mongo dependency, unit-testable in
/// isolation against the exact contract example.
/// </summary>
public static class SimulationCalculator
{
    public const decimal Rate = 0.018m;

    public record SimulationResult(decimal InstallmentAmount, decimal TotalAmount);

    public static SimulationResult Calculate(decimal requestedAmount, int installments)
    {
        var factor = (double)Math.Pow((double)(1 + Rate), installments);
        var installmentAmountRaw = requestedAmount * (Rate * (decimal)factor) / (decimal)(factor - 1);
        var installmentAmount = Math.Round(installmentAmountRaw, 2, MidpointRounding.AwayFromZero);

        // totalAmount is the product of the already-rounded installment (README seção 4.2),
        // not the raw pre-rounding value.
        var totalAmount = Math.Round(installmentAmount * installments, 2, MidpointRounding.AwayFromZero);

        return new SimulationResult(installmentAmount, totalAmount);
    }
}
