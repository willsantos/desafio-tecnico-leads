using ConsignadoLeads.Api.Features.Identification;

namespace ConsignadoLeads.Api.Tests.Unit.Features;

/// <summary>
/// Unit coverage for <see cref="IdentificationValidator"/>. Pure function; the integration suite
/// only exercises the happy path, so these tests drive every missing-field branch plus the
/// address-null and invalid-document-type branches.
/// </summary>
[Trait("Category", "Unit")]
public class IdentificationValidatorTests
{
    private static AddressRequest ValidAddress() => new(
        ZipCode: "01000-000",
        Street: "Rua A",
        Number: "123",
        Complement: null,
        Neighborhood: "Centro",
        City: "São Paulo",
        State: "SP");

    private static IdentificationRequest ValidRequest() => new(
        FullName: "Ana Silva",
        Cpf: "12345678901",
        BirthDate: "1990-01-01",
        Email: "ana@example.com",
        Phone: "11999999999",
        Address: ValidAddress(),
        MotherName: "Maria Silva",
        MaritalStatus: "single",
        DocumentType: "CNH",
        DocumentNumber: "AB123456",
        IssuingAuthority: "DETRAN",
        IssuingState: "SP",
        IssueDate: "2015-01-01",
        MockOutcome: null,
        ExpectedVersion: null);

    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        Assert.Empty(IdentificationValidator.Validate(ValidRequest()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingFullName_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { FullName = value! });
        Assert.Contains("fullName é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingCpf_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { Cpf = value! });
        Assert.Contains("cpf é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingBirthDate_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { BirthDate = value! });
        Assert.Contains("birthDate é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingEmail_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { Email = value! });
        Assert.Contains("email é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingPhone_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { Phone = value! });
        Assert.Contains("phone é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingMotherName_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { MotherName = value! });
        Assert.Contains("motherName é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingMaritalStatus_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { MaritalStatus = value! });
        Assert.Contains("maritalStatus é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null, "missing")]
    [InlineData("", "empty")]
    [InlineData("PASSPORT", "invalid value")]
    public void Validate_InvalidDocumentType_AddsError(string? value, string _)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { DocumentType = value! });
        Assert.Contains("documentType é obrigatório e deve ser CNH ou RG.", errors);
    }

    [Theory]
    [InlineData("CNH")]
    [InlineData("RG")]
    public void Validate_EachValidDocumentType_Passes(string documentType)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { DocumentType = documentType });
        Assert.DoesNotContain(errors, e => e.Contains("documentType"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingDocumentNumber_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { DocumentNumber = value! });
        Assert.Contains("documentNumber é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingIssuingAuthority_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { IssuingAuthority = value! });
        Assert.Contains("issuingAuthority é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingIssuingState_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { IssuingState = value! });
        Assert.Contains("issuingState é obrigatório.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingIssueDate_AddsError(string? value)
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { IssueDate = value! });
        Assert.Contains("issueDate é obrigatório.", errors);
    }

    [Fact]
    public void Validate_NullAddress_AddsOnlyAddressError()
    {
        var errors = IdentificationValidator.Validate(ValidRequest() with { Address = null! });
        Assert.Single(errors);
        Assert.Contains("address é obrigatório.", errors);
    }

    [Fact]
    public void Validate_AddressMissingEveryField_ReturnsAllAddressErrors()
    {
        var empty = new AddressRequest("", "", "", null, "", "", "");
        var errors = IdentificationValidator.Validate(ValidRequest() with { Address = empty });
        Assert.Equal(6, errors.Count);
        Assert.Contains("address.zipCode é obrigatório.", errors);
        Assert.Contains("address.street é obrigatório.", errors);
        Assert.Contains("address.number é obrigatório.", errors);
        Assert.Contains("address.neighborhood é obrigatório.", errors);
        Assert.Contains("address.city é obrigatório.", errors);
        Assert.Contains("address.state é obrigatório.", errors);
    }
}
