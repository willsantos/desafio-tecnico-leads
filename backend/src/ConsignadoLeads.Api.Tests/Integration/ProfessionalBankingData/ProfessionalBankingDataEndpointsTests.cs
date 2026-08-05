using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConsignadoLeads.Api.Core.Dtos;

namespace ConsignadoLeads.Api.Tests.Integration.ProfessionalBankingData;

[Trait("Category", "Integration")]
[Collection(nameof(ProfessionalBankingDataEndpointsTests))]
public class ProfessionalBankingDataEndpointsTests : IClassFixture<LeadRecoveryWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProfessionalBankingDataEndpointsTests(LeadRecoveryWebApplicationFactory factory)
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

    /// <summary>Progresses a lead through etapas 1-3 (consultation, simulation, identification)
    /// so the "must not erase prior steps" assertion has real data to check against.</summary>
    private async Task<string> CreateLeadThroughIdentificationAsync()
    {
        var leadId = await CreateLeadAsync();

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

        return leadId;
    }

    private static object ValidBody(int? expectedVersion = null, string? pixKey = "chave@pix.com") => new
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
            pixKey,
        },
        expectedVersion,
    };

    [Fact]
    public async Task PutProfessionalBankingData_WithValidPayload_Returns200AndPersistsData()
    {
        var leadId = await CreateLeadAsync();

        var response = await _client.PutAsJsonAsync($"/leads/{leadId}/steps/professional-banking-data", ValidBody());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal("ACME", dto!.ProfessionalData!.Company);
        Assert.Equal("001", dto.BankingData!.Bank);
        Assert.Equal("chave@pix.com", dto.BankingData.PixKey);
    }

    [Fact]
    public async Task PutProfessionalBankingData_AfterEarlierSteps_DoesNotEraseConsultationSimulationsIdentification()
    {
        var leadId = await CreateLeadThroughIdentificationAsync();

        var response = await _client.PutAsJsonAsync($"/leads/{leadId}/steps/professional-banking-data", ValidBody());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();

        // The named spec AC (P1-4 Independent Test): this update must not wipe out data
        // already recorded by earlier steps on the same lead.
        Assert.NotNull(dto!.Consultation);
        Assert.NotNull(dto.Consultation!.Result);
        Assert.Equal("eligible", dto.Consultation.Result!.Outcome);
        Assert.Single(dto.Simulations);
        Assert.Equal(516.81m, dto.Simulations[0].InstallmentAmount);
        Assert.NotNull(dto.Identification);
        Assert.Equal("Ana Silva", dto.Identification!.FullName);
        Assert.Equal("CNH", dto.Identification.DocumentType);

        // And the new data was actually persisted alongside the preserved data.
        Assert.Equal("ACME", dto.ProfessionalData!.Company);
        Assert.Equal("001", dto.BankingData!.Bank);
    }

    [Fact]
    public async Task PutProfessionalBankingData_WithoutPixKey_IsAcceptedWithoutError()
    {
        var leadId = await CreateLeadAsync();

        var response = await _client.PutAsJsonAsync($"/leads/{leadId}/steps/professional-banking-data", ValidBody(pixKey: null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Null(dto!.BankingData!.PixKey);
    }

    [Fact]
    public async Task PutProfessionalBankingData_WhenLeadDoesNotExist_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/leads/{Guid.NewGuid()}/steps/professional-banking-data", ValidBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutProfessionalBankingData_WhenExpectedVersionMismatches_Returns409WithCurrentVersion()
    {
        var leadId = await CreateLeadAsync();

        var response = await _client.PutAsJsonAsync($"/leads/{leadId}/steps/professional-banking-data", ValidBody(expectedVersion: 999));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problemJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(problemJson);
        Assert.Equal(1, doc.RootElement.GetProperty("extensions").GetProperty("currentVersion").GetInt32());
    }
}
