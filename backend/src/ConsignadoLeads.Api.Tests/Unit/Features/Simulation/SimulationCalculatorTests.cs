using ConsignadoLeads.Api.Features.Simulation;

namespace ConsignadoLeads.Api.Tests.Unit.Features.Simulation;

[Trait("Category", "Unit")]
public class SimulationCalculatorTests
{
    [Fact]
    public void Calculate_WithReadmeExample_MatchesExactContractValues()
    {
        // README seção 4.2 worked example: requestedAmount=10000, installments=24 -> 516.81 / 12403.44
        var result = SimulationCalculator.Calculate(requestedAmount: 10000m, installments: 24);

        Assert.Equal(516.81m, result.InstallmentAmount);
        Assert.Equal(12403.44m, result.TotalAmount);
    }

    [Fact]
    public void Calculate_TotalAmount_UsesRoundedInstallmentNotRawValue()
    {
        // If totalAmount used the raw (unrounded) installment, it would differ from
        // installmentAmount * installments at 2-decimal precision for at least some inputs.
        var result = SimulationCalculator.Calculate(requestedAmount: 10000m, installments: 24);

        Assert.Equal(Math.Round(result.InstallmentAmount * 24, 2), result.TotalAmount);
    }

    [Fact]
    public void Calculate_RoundsInstallmentAmountToTwoDecimals()
    {
        var result = SimulationCalculator.Calculate(requestedAmount: 5000m, installments: 12);

        Assert.Equal(Math.Round(result.InstallmentAmount, 2), result.InstallmentAmount);
    }
}
