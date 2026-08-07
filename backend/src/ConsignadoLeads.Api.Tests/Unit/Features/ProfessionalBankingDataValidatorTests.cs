using ConsignadoLeads.Api.Features.ProfessionalBankingData;

namespace ConsignadoLeads.Api.Tests.Unit.Features;

/// <summary>
/// Unit coverage for <see cref="ProfessionalBankingDataValidator"/>. Pure function; the
/// integration suite only exercises the happy path, so these tests drive every missing-field
/// branch plus the null-sub-object branches and the monthlyIncome boundary.
/// </summary>
[Trait("Category", "Unit")]
public class ProfessionalBankingDataValidatorTests
{
    private static ProfessionalDataRequest ValidProfessional() => new(
        EmploymentType: "clt",
        Company: "ACME",
        RegistrationNumber: "REG-1",
        Role: "Analyst",
        MonthlyIncome: 5000m,
        AdmissionDate: "2020-01-01");

    private static BankingDataRequest ValidBanking() => new(
        Bank: "001",
        Agency: "1234",
        Account: "56789",
        AccountDigit: "0",
        AccountType: "corrente",
        AccountHolder: "Ana Silva",
        PixKey: null);

    private static ProfessionalBankingDataRequest ValidRequest() => new(
        ValidProfessional(),
        ValidBanking(),
        ExpectedVersion: null);

    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        Assert.Empty(ProfessionalBankingDataValidator.Validate(ValidRequest()));
    }

    [Fact]
    public void Validate_NullProfessionalData_AddsOnlyThatError()
    {
        var request = ValidRequest() with { ProfessionalData = null! };
        var errors = ProfessionalBankingDataValidator.Validate(request);
        Assert.Single(errors);
        Assert.Contains("professionalData é obrigatório.", errors);
    }

    [Fact]
    public void Validate_NullBankingData_AddsOnlyThatError()
    {
        var request = ValidRequest() with { BankingData = null! };
        var errors = ProfessionalBankingDataValidator.Validate(request);
        Assert.Single(errors);
        Assert.Contains("bankingData é obrigatório.", errors);
    }

    [Fact]
    public void Validate_NullBothSubObjects_ReturnsTwoErrors()
    {
        var request = new ProfessionalBankingDataRequest(null!, null!, null);
        var errors = ProfessionalBankingDataValidator.Validate(request);
        Assert.Equal(2, errors.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingEmploymentType_AddsError(string? value)
    {
        var request = ValidRequest() with { ProfessionalData = ValidProfessional() with { EmploymentType = value! } };
        Assert.Contains("professionalData.employmentType é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingCompany_AddsError(string? value)
    {
        var request = ValidRequest() with { ProfessionalData = ValidProfessional() with { Company = value! } };
        Assert.Contains("professionalData.company é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingRegistrationNumber_AddsError(string? value)
    {
        var request = ValidRequest() with { ProfessionalData = ValidProfessional() with { RegistrationNumber = value! } };
        Assert.Contains("professionalData.registrationNumber é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingRole_AddsError(string? value)
    {
        var request = ValidRequest() with { ProfessionalData = ValidProfessional() with { Role = value! } };
        Assert.Contains("professionalData.role é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Validate_MonthlyIncomeNotPositive_AddsError(decimal income)
    {
        var request = ValidRequest() with { ProfessionalData = ValidProfessional() with { MonthlyIncome = income } };
        Assert.Contains("professionalData.monthlyIncome deve ser maior que zero.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingAdmissionDate_AddsError(string? value)
    {
        var request = ValidRequest() with { ProfessionalData = ValidProfessional() with { AdmissionDate = value! } };
        Assert.Contains("professionalData.admissionDate é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingBank_AddsError(string? value)
    {
        var request = ValidRequest() with { BankingData = ValidBanking() with { Bank = value! } };
        Assert.Contains("bankingData.bank é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingAgency_AddsError(string? value)
    {
        var request = ValidRequest() with { BankingData = ValidBanking() with { Agency = value! } };
        Assert.Contains("bankingData.agency é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingAccount_AddsError(string? value)
    {
        var request = ValidRequest() with { BankingData = ValidBanking() with { Account = value! } };
        Assert.Contains("bankingData.account é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingAccountDigit_AddsError(string? value)
    {
        var request = ValidRequest() with { BankingData = ValidBanking() with { AccountDigit = value! } };
        Assert.Contains("bankingData.accountDigit é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingAccountType_AddsError(string? value)
    {
        var request = ValidRequest() with { BankingData = ValidBanking() with { AccountType = value! } };
        Assert.Contains("bankingData.accountType é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingAccountHolder_AddsError(string? value)
    {
        var request = ValidRequest() with { BankingData = ValidBanking() with { AccountHolder = value! } };
        Assert.Contains("bankingData.accountHolder é obrigatório.", ProfessionalBankingDataValidator.Validate(request));
    }
}
