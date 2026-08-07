using ConsignadoLeads.Api.Features.Consultation;

namespace ConsignadoLeads.Api.Tests.Unit.Features;

/// <summary>
/// Unit coverage for <see cref="ConsultationValidator"/>. The validator is a pure function; the
/// integration suite only exercises its happy path, so these tests drive every error branch.
/// </summary>
[Trait("Category", "Unit")]
public class ConsultationValidatorTests
{
    private static ConsultationRequest ValidRequest() => new(
        Cpf: "12345678901",
        BirthDate: "1990-01-01",
        BenefitType: "retirement",
        BenefitNumber: "12345",
        PayingInstitution: "INSS",
        ConsultationAuthorized: true,
        MockOutcome: null,
        ExpectedVersion: null);

    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        var errors = ConsultationValidator.Validate(ValidRequest());
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingCpf_AddsError(string? cpf)
    {
        var request = ValidRequest() with { Cpf = cpf };
        var errors = ConsultationValidator.Validate(request);
        Assert.Contains("cpf é obrigatório e deve conter 11 dígitos.", errors);
    }

    [Theory]
    [InlineData("123", "too short")]
    [InlineData("123456789012", "too long")]
    [InlineData("123456789a", "non-digit")]
    [InlineData("12.345.678-9", "masked")]
    public void Validate_MalformedCpf_AddsError(string cpf, string _)
    {
        var request = ValidRequest() with { Cpf = cpf };
        var errors = ConsultationValidator.Validate(request);
        Assert.Contains("cpf é obrigatório e deve conter 11 dígitos.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingBirthDate_AddsError(string? birthDate)
    {
        var request = ValidRequest() with { BirthDate = birthDate };
        var errors = ConsultationValidator.Validate(request);
        Assert.Contains("birthDate é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void Validate_InvalidBenefitType_AddsError(string? benefitType)
    {
        var request = ValidRequest() with { BenefitType = benefitType };
        var errors = ConsultationValidator.Validate(request);
        Assert.Contains("benefitType é obrigatório e deve ser um dos valores válidos.", errors);
    }

    [Fact]
    public void Validate_EachValidBenefitType_Passes()
    {
        foreach (var benefitType in new[] { "retirement", "pension", "public_servant", "clt" })
        {
            var errors = ConsultationValidator.Validate(ValidRequest() with { BenefitType = benefitType });
            Assert.DoesNotContain(errors, e => e.Contains("benefitType"));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingBenefitNumber_AddsError(string? benefitNumber)
    {
        var request = ValidRequest() with { BenefitNumber = benefitNumber };
        var errors = ConsultationValidator.Validate(request);
        Assert.Contains("benefitNumber é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingPayingInstitution_AddsError(string? payingInstitution)
    {
        var request = ValidRequest() with { PayingInstitution = payingInstitution };
        var errors = ConsultationValidator.Validate(request);
        Assert.Contains("payingInstitution é obrigatório.", errors);
    }

    [Fact]
    public void Validate_ConsultationAuthorizedFalse_AddsError()
    {
        var request = ValidRequest() with { ConsultationAuthorized = false };
        var errors = ConsultationValidator.Validate(request);
        Assert.Contains("consultationAuthorized deve ser true.", errors);
    }

    [Fact]
    public void Validate_ConsultationAuthorizedNull_AddsError()
    {
        var request = ValidRequest() with { ConsultationAuthorized = null };
        var errors = ConsultationValidator.Validate(request);
        Assert.Contains("consultationAuthorized deve ser true.", errors);
    }

    [Fact]
    public void Validate_AllFieldsMissing_ReturnsEveryError()
    {
        var request = new ConsultationRequest(
            Cpf: null, BirthDate: null, BenefitType: null, BenefitNumber: null,
            PayingInstitution: null, ConsultationAuthorized: null, MockOutcome: null, ExpectedVersion: null);
        var errors = ConsultationValidator.Validate(request);
        Assert.Equal(6, errors.Count);
    }
}
