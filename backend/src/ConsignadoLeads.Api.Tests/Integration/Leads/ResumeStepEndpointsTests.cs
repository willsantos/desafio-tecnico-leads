using System.Net.Http.Json;
using ConsignadoLeads.Api.Core.Dtos;

namespace ConsignadoLeads.Api.Tests.Integration.Leads;

/// <summary>
/// End-to-end coverage for the derived <c>progress.resumeStep</c> field across lead states.
/// Each test drives a lead through real endpoints and asserts the spec-defined resumeStep value
/// (<c>.specs/features/progress-resume-step/spec.md</c> PRS-01/04/05/07/08).
/// </summary>
[Trait("Category", "Integration")]
[Collection(nameof(ResumeStepEndpointsTests))]
public class ResumeStepEndpointsTests : IClassFixture<LeadRecoveryWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ResumeStepEndpointsTests(LeadRecoveryWebApplicationFactory factory)
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
        var dto = await _client.PostAsJsonAsync("/leads/consultation", body);
        var lead = (await dto.Content.ReadFromJsonAsync<LeadDto>())!;
        return lead.Id;
    }

    private async Task ProgressThroughIdentificationAsync(string leadId)
    {
        await _client.PostAsJsonAsync($"/leads/{leadId}/steps/simulation", new { requestedAmount = 10000m, installments = 24 });

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
            documentType = "CNH",
            documentNumber = "AB123456",
            issuingAuthority = "DETRAN",
            issuingState = "SP",
            issueDate = "2015-01-01",
        };
        await _client.PutAsJsonAsync($"/leads/{leadId}/steps/identification", identificationBody);
    }

    private async Task ProgressThroughProfessionalBankingDataAsync(string leadId)
    {
        var body = new
        {
            professionalData = new
            {
                employmentType = "clt",
                company = "ACME",
                registrationNumber = "REG-1",
                role = "Analyst",
                monthlyIncome = 5000m,
                admissionDate = "2020-01-01",
            },
            bankingData = new
            {
                bank = "001",
                agency = "1234",
                account = "56789",
                accountDigit = "0",
                accountType = "checking",
                accountHolder = "Ana Silva",
                pixKey = "chave@pix.com",
            },
        };
        await _client.PutAsJsonAsync($"/leads/{leadId}/steps/professional-banking-data", body);
    }

    private static MultipartFormDataContent PersonalDocumentForm()
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "cnh.jpg");
        form.Add(new StringContent("personal_document"), "type");
        form.Add(new StringContent("CNH"), "personalDocumentSubtype");
        return form;
    }

    private static MultipartFormDataContent PayslipForm()
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent([4, 5, 6]);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "payslip.pdf");
        form.Add(new StringContent("payslip"), "type");
        return form;
    }

    // PRS-01: resumeStep is always present (non-empty) in the full LeadDto.
    [Fact]
    public async Task GetLeadById_AlwaysReturnsResumeStep()
    {
        var leadId = await CreateLeadAsync();

        var dto = await _client.GetFromJsonAsync<LeadDto>($"/leads/{leadId}");

        Assert.False(string.IsNullOrWhiteSpace(dto!.Progress.ResumeStep));
    }

    // PRS-03 (integration mirror): after consultation only, resumeStep = "simulation".
    [Fact]
    public async Task AfterConsultation_ResumeStep_IsSimulation()
    {
        var leadId = await CreateLeadAsync();

        var dto = await _client.GetFromJsonAsync<LeadDto>($"/leads/{leadId}");

        Assert.Equal("simulation", dto!.Progress.ResumeStep);
    }

    // PRS-04: all backend steps done, no documents -> "documents".
    [Fact]
    public async Task AfterProfessionalBankingData_NoDocuments_ResumeStep_IsDocuments()
    {
        var leadId = await CreateLeadAsync();
        await ProgressThroughIdentificationAsync(leadId);
        await ProgressThroughProfessionalBankingDataAsync(leadId);

        var dto = await _client.GetFromJsonAsync<LeadDto>($"/leads/{leadId}");

        Assert.Equal("documents", dto!.Progress.ResumeStep);
    }

    // PRS-04 (also covers the PUT response surface): the professional-banking-data PUT response
    // itself returns resumeStep="documents" when no documents were uploaded yet.
    [Fact]
    public async Task ProfessionalBankingDataPutResponse_NoDocuments_ResumeStep_IsDocuments()
    {
        var leadId = await CreateLeadAsync();
        await ProgressThroughIdentificationAsync(leadId);

        var body = new
        {
            professionalData = new
            {
                employmentType = "clt",
                company = "ACME",
                registrationNumber = "REG-1",
                role = "Analyst",
                monthlyIncome = 5000m,
                admissionDate = "2020-01-01",
            },
            bankingData = new
            {
                bank = "001",
                agency = "1234",
                account = "56789",
                accountDigit = "0",
                accountType = "checking",
                accountHolder = "Ana Silva",
                pixKey = "chave@pix.com",
            },
        };
        var response = await _client.PutAsJsonAsync($"/leads/{leadId}/steps/professional-banking-data", body);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();

        Assert.Equal("documents", dto!.Progress.ResumeStep);
    }

    // PRS-05: all backend steps done + both documents -> "confirmation".
    [Fact]
    public async Task AfterProfessionalBankingData_AndBothDocuments_ResumeStep_IsConfirmation()
    {
        var leadId = await CreateLeadAsync();
        await ProgressThroughIdentificationAsync(leadId);
        await ProgressThroughProfessionalBankingDataAsync(leadId);
        await _client.PostAsync($"/leads/{leadId}/documents", PersonalDocumentForm());
        await _client.PostAsync($"/leads/{leadId}/documents", PayslipForm());

        var dto = await _client.GetFromJsonAsync<LeadDto>($"/leads/{leadId}");

        Assert.Equal("confirmation", dto!.Progress.ResumeStep);
    }

    // PRS-07: existing progress fields keep their current values/semantics (non-breaking).
    [Fact]
    public async Task ProgressFields_RemainUnchangedAfterFeature()
    {
        var leadId = await CreateLeadAsync();
        await ProgressThroughIdentificationAsync(leadId);

        var dto = await _client.GetFromJsonAsync<LeadDto>($"/leads/{leadId}");

        // currentStep still names the last completed step; completedSteps still lists them in order.
        Assert.Equal("identification", dto!.Progress.CurrentStep);
        Assert.Equal(new[] { "consultation", "simulation", "identification" }, dto.Progress.CompletedSteps);
        Assert.NotEqual(default, dto.Progress.LastUpdatedAt);
    }

    // Edge: only one document type uploaded -> still "documents".
    [Fact]
    public async Task AfterProfessionalBankingData_OnlyPersonalDocument_ResumeStep_IsDocuments()
    {
        var leadId = await CreateLeadAsync();
        await ProgressThroughIdentificationAsync(leadId);
        await ProgressThroughProfessionalBankingDataAsync(leadId);
        await _client.PostAsync($"/leads/{leadId}/documents", PersonalDocumentForm());

        var dto = await _client.GetFromJsonAsync<LeadDto>($"/leads/{leadId}");

        Assert.Equal("documents", dto!.Progress.ResumeStep);
    }
}
