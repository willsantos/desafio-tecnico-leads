using ConsignadoLeads.Api.Core.Logging;

namespace ConsignadoLeads.Api.Tests.Unit.Core;

[Trait("Category", "Unit")]
public class SensitiveDataMaskerTests
{
    private record BankingData(string Bank, string Account, string Agency);

    private record LeadLike(string Cpf, string? DocumentNumber, string FullName, BankingData? BankingData);

    [Fact]
    public void Mask_WhenCpfFieldPresent_RedactsCpfValue()
    {
        var value = new LeadLike(Cpf: "12345678900", DocumentNumber: null, FullName: "Ana", BankingData: null);

        var masked = SensitiveDataMasker.Mask(value);

        Assert.DoesNotContain("12345678900", masked);
        Assert.Contains("***REDACTED***", masked);
    }

    [Fact]
    public void Mask_WhenSensitiveFieldAbsent_LeavesOtherFieldsUnredacted()
    {
        var value = new LeadLike(Cpf: "12345678900", DocumentNumber: null, FullName: "Ana", BankingData: null);

        var masked = SensitiveDataMasker.Mask(value);

        Assert.Contains("Ana", masked);
    }

    [Fact]
    public void Mask_WhenBankingDataNested_RedactsEntireNestedObject()
    {
        var value = new LeadLike(
            Cpf: "12345678900",
            DocumentNumber: "AB123456",
            FullName: "Ana",
            BankingData: new BankingData(Bank: "001", Account: "98765-4", Agency: "1234"));

        var masked = SensitiveDataMasker.Mask(value);

        Assert.DoesNotContain("98765-4", masked);
        Assert.DoesNotContain("1234", masked);
    }

    [Fact]
    public void Mask_WhenDocumentNumberPresent_RedactsDocumentNumberValue()
    {
        var value = new LeadLike(Cpf: "12345678900", DocumentNumber: "AB123456", FullName: "Ana", BankingData: null);

        var masked = SensitiveDataMasker.Mask(value);

        Assert.DoesNotContain("AB123456", masked);
    }
}
