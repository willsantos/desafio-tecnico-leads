using System.Net;
using System.Net.Http.Json;
using ConsignadoLeads.Api.Core.Dtos;

namespace ConsignadoLeads.Api.Tests.Integration.Documents;

[Trait("Category", "Integration")]
[Collection(nameof(DocumentsEndpointsTests))]
public class DocumentsEndpointsTests : IClassFixture<LeadRecoveryWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentsEndpointsTests(LeadRecoveryWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string RandomCpf() => Random.Shared.NextInt64(10_000_000_000, 99_999_999_999).ToString();

    private async Task<string> CreateLeadAsync()
    {
        var body = new
        {
            cpf = RandomCpf(),
            birthDate = "1990-01-01",
            benefitType = "retirement",
            benefitNumber = "12345",
            payingInstitution = "INSS",
            consultationAuthorized = true,
        };

        var response = await _client.PostAsJsonAsync("/leads/consultation", body);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        return dto!.Id;
    }

    private async Task<string> CreateLeadWithIdentificationAsync(string documentType = "CNH")
    {
        var leadId = await CreateLeadAsync();
        var identificationBody = new
        {
            fullName = "Ana Silva",
            cpf = RandomCpf(),
            birthDate = "1990-01-01",
            email = "ana@example.com",
            phone = "11999999999",
            address = new { zipCode = "01000-000", street = "Rua A", number = "123", neighborhood = "Centro", city = "São Paulo", state = "SP" },
            motherName = "Maria Silva",
            maritalStatus = "single",
            documentType,
            documentNumber = "AB123456",
            issuingAuthority = "DETRAN",
            issuingState = "SP",
            issueDate = "2015-01-01",
        };
        await _client.PutAsJsonAsync($"/leads/{leadId}/steps/identification", identificationBody);
        return leadId;
    }

    private static MultipartFormDataContent BuildUploadForm(byte[] bytes, string contentType, string type, string? subtype = null, string fileName = "doc.jpg")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(type), "type");
        if (subtype is not null)
        {
            content.Add(new StringContent(subtype), "personalDocumentSubtype");
        }

        return content;
    }

    [Fact]
    public async Task PostDocument_WithValidPayslip_Returns201Uploaded()
    {
        var leadId = await CreateLeadAsync();
        using var form = BuildUploadForm([1, 2, 3], "application/pdf", "payslip", fileName: "payslip.pdf");

        var response = await _client.PostAsync($"/leads/{leadId}/documents", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.Equal("uploaded", dto!.Status);
        Assert.Equal("payslip", dto.Type);
    }

    [Fact]
    public async Task PostDocument_WhenLeadDoesNotExist_Returns404AndCreatesNoLead()
    {
        var missingLeadId = Guid.NewGuid().ToString();
        using var form = BuildUploadForm([1, 2, 3], "application/pdf", "payslip", fileName: "payslip.pdf");

        var response = await _client.PostAsync($"/leads/{missingLeadId}/documents", form);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var getLeadResponse = await _client.GetAsync($"/leads/{missingLeadId}");
        Assert.Equal(HttpStatusCode.NotFound, getLeadResponse.StatusCode);
    }

    [Fact]
    public async Task PostDocument_ExceedingSizeLimit_Returns400()
    {
        var leadId = await CreateLeadAsync();
        var oversized = new byte[DocumentsValidatorSize + 1];
        using var form = BuildUploadForm(oversized, "application/pdf", "payslip", fileName: "payslip.pdf");

        var response = await _client.PostAsync($"/leads/{leadId}/documents", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDocument_WithInvalidContentType_Returns400()
    {
        var leadId = await CreateLeadAsync();
        using var form = BuildUploadForm([1, 2, 3], "text/plain", "payslip", fileName: "payslip.txt");

        var response = await _client.PostAsync($"/leads/{leadId}/documents", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostDocument_ReuploadSameType_MarksPreviousReplacedAndExcludesFromActiveList()
    {
        var leadId = await CreateLeadAsync();
        using var firstForm = BuildUploadForm([1, 2, 3], "application/pdf", "payslip", fileName: "payslip-v1.pdf");
        var firstResponse = await _client.PostAsync($"/leads/{leadId}/documents", firstForm);
        var first = await firstResponse.Content.ReadFromJsonAsync<DocumentDto>();

        using var secondForm = BuildUploadForm([4, 5, 6], "application/pdf", "payslip", fileName: "payslip-v2.pdf");
        var secondResponse = await _client.PostAsync($"/leads/{leadId}/documents", secondForm);
        var second = await secondResponse.Content.ReadFromJsonAsync<DocumentDto>();

        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/leads/{leadId}/documents");
        var active = await listResponse.Content.ReadFromJsonAsync<List<DocumentDto>>();

        Assert.DoesNotContain(active!, d => d.Id == first!.Id);
        Assert.Contains(active!, d => d.Id == second!.Id);
    }

    [Fact]
    public async Task PostDocument_PersonalDocumentSubtypeMismatchingIdentification_IsAcceptedAndRecordedAsIs()
    {
        // Etapa 3 informou CNH; etapa 5 envia RG. The mismatch is not silently rejected or
        // normalized here — it's recorded verbatim so the confirm-time validator (T13) can
        // surface it as a blocking pendency (spec P1-5 AC5, Cenário 8).
        var leadId = await CreateLeadWithIdentificationAsync(documentType: "CNH");
        using var form = BuildUploadForm([1, 2, 3], "image/jpeg", "personal_document", subtype: "RG", fileName: "rg.jpg");

        var response = await _client.PostAsync($"/leads/{leadId}/documents", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.Equal("RG", dto!.PersonalDocumentSubtype);

        var listResponse = await _client.GetAsync($"/leads/{leadId}/documents");
        var active = await listResponse.Content.ReadFromJsonAsync<List<DocumentDto>>();
        Assert.Contains(active!, d => d.Id == dto.Id && d.PersonalDocumentSubtype == "RG");
    }

    [Fact]
    public async Task PostDocument_SeparateUploadsWithAbandonmentBetween_BothPersistAndAppearInActiveList()
    {
        var leadId = await CreateLeadAsync();

        using var personalForm = BuildUploadForm([1, 2, 3], "image/jpeg", "personal_document", subtype: "RG", fileName: "rg.jpg");
        await _client.PostAsync($"/leads/{leadId}/documents", personalForm);

        // Simulates the client abandoning between uploads — a fresh call later, no shared state.
        using var payslipForm = BuildUploadForm([4, 5, 6], "application/pdf", "payslip", fileName: "payslip.pdf");
        await _client.PostAsync($"/leads/{leadId}/documents", payslipForm);

        var listResponse = await _client.GetAsync($"/leads/{leadId}/documents");
        var active = await listResponse.Content.ReadFromJsonAsync<List<DocumentDto>>();

        Assert.Contains(active!, d => d.Type == "personal_document");
        Assert.Contains(active!, d => d.Type == "payslip");
    }

    [Fact]
    public async Task GetDocuments_ExcludesDeletedAndReplaced()
    {
        var leadId = await CreateLeadAsync();
        using var form1 = BuildUploadForm([1, 2, 3], "application/pdf", "payslip", fileName: "v1.pdf");
        var response1 = await _client.PostAsync($"/leads/{leadId}/documents", form1);
        var doc1 = await response1.Content.ReadFromJsonAsync<DocumentDto>();

        using var form2 = BuildUploadForm([4, 5, 6], "image/jpeg", "personal_document", subtype: "RG", fileName: "rg.jpg");
        var response2 = await _client.PostAsync($"/leads/{leadId}/documents", form2);
        var doc2 = await response2.Content.ReadFromJsonAsync<DocumentDto>();

        var deleteResponse = await _client.DeleteAsync($"/leads/{leadId}/documents/{doc2!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/leads/{leadId}/documents");
        var active = await listResponse.Content.ReadFromJsonAsync<List<DocumentDto>>();

        Assert.Contains(active!, d => d.Id == doc1!.Id);
        Assert.DoesNotContain(active!, d => d.Id == doc2.Id);
    }

    [Fact]
    public async Task DeleteDocument_WhenMissing_Returns404()
    {
        var leadId = await CreateLeadAsync();

        var response = await _client.DeleteAsync($"/leads/{leadId}/documents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDocuments_WhenLeadDoesNotExist_Returns404()
    {
        var response = await _client.GetAsync($"/leads/{Guid.NewGuid()}/documents");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private const int DocumentsValidatorSize = 10 * 1024 * 1024;
}
