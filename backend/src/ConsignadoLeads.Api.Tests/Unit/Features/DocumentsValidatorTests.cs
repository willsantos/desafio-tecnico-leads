using System.Text;
using ConsignadoLeads.Api.Features.Documents;
using Microsoft.AspNetCore.Http;

namespace ConsignadoLeads.Api.Tests.Unit.Features;

/// <summary>
/// Unit coverage for <see cref="DocumentsValidator"/>. Pure function over an IFormFile; the
/// integration suite covers the happy upload, so these tests drive the null-file, oversize,
/// bad-content-type, invalid-type and missing-subtype branches.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentsValidatorTests
{
    /// <summary>Minimal IFormFile stub: the validator only reads Length and ContentType.</summary>
    private sealed class FakeFormFile : IFormFile
    {
        public required string ContentType { get; init; }
        public required long Length { get; init; }
        public string FileName => "f.jpg";
        public string Name => "file";
        public string? ContentDisposition => null;
        public IHeaderDictionary Headers => new HeaderDictionary();
        public void CopyTo(Stream target) { }
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Stream OpenReadStream() => new MemoryStream(Encoding.UTF8.GetBytes("x"));
    }

    private static FakeFormFile MakeFile(long length = 1024, string contentType = "image/jpeg") => new()
    {
        Length = length,
        ContentType = contentType,
    };

    [Fact]
    public void Validate_ValidPayslip_ReturnsNoErrors()
    {
        Assert.Empty(DocumentsValidator.Validate(MakeFile(), "payslip", null));
    }

    [Fact]
    public void Validate_ValidPersonalDocumentWithSubtype_ReturnsNoErrors()
    {
        Assert.Empty(DocumentsValidator.Validate(MakeFile(), "personal_document", "CNH"));
    }

    [Fact]
    public void Validate_NullFile_AddsError()
    {
        var errors = DocumentsValidator.Validate(null, "payslip", null);
        Assert.Contains("file é obrigatório.", errors);
    }

    [Fact]
    public void Validate_FileOverLimit_AddsError()
    {
        var file = MakeFile(length: DocumentsValidator.MaxSizeBytes + 1);
        var errors = DocumentsValidator.Validate(file, "payslip", null);
        Assert.Contains("Arquivo excede o limite de 10MB.", errors);
    }

    [Fact]
    public void Validate_FileAtExactLimit_Passes()
    {
        var file = MakeFile(length: DocumentsValidator.MaxSizeBytes);
        Assert.Empty(DocumentsValidator.Validate(file, "payslip", null));
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("image/gif")]
    [InlineData("application/octet-stream")]
    public void Validate_FileWithDisallowedContentType_AddsError(string contentType)
    {
        var file = MakeFile(contentType: contentType);
        var errors = DocumentsValidator.Validate(file, "payslip", null);
        Assert.Contains(errors, e => e.Contains("Tipo de conteúdo inválido"));
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("application/pdf")]
    public void Validate_EachAllowedContentType_Passes(string contentType)
    {
        var file = MakeFile(contentType: contentType);
        Assert.Empty(DocumentsValidator.Validate(file, "payslip", null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("contract")]
    public void Validate_InvalidType_AddsError(string? type)
    {
        var errors = DocumentsValidator.Validate(MakeFile(), type, null);
        Assert.Contains("type é obrigatório e deve ser personal_document ou payslip.", errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_PersonalDocumentWithoutSubtype_AddsError(string? subtype)
    {
        var errors = DocumentsValidator.Validate(MakeFile(), "personal_document", subtype);
        Assert.Contains("personalDocumentSubtype é obrigatório quando type=personal_document.", errors);
    }

    [Fact]
    public void Validate_PayslipIgnoresSubtype()
    {
        // subtype is only required for personal_document; a payslip with a stray subtype value
        // must still validate (distinct from the null-subtype happy path).
        Assert.Empty(DocumentsValidator.Validate(MakeFile(), "payslip", "CNH"));
    }

    [Fact]
    public void Validate_NullEverything_ReturnsFileAndTypeErrors()
    {
        var errors = DocumentsValidator.Validate(null, null, null);
        Assert.Equal(2, errors.Count);
    }
}
