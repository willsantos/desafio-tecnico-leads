using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConsignadoLeads.Api.Core.Dtos;

namespace ConsignadoLeads.Api.Tests.Integration.Identification;

[Trait("Category", "Integration")]
[Collection(nameof(IdentificationEndpointsTests))]
public class IdentificationEndpointsTests : IClassFixture<LeadRecoveryWebApplicationFactory>
{
    private readonly LeadRecoveryWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IdentificationEndpointsTests(LeadRecoveryWebApplicationFactory factory)
    {
        _factory = factory;
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

    private static object ValidIdentificationBody(string? mockOutcome = null, int? expectedVersion = null) => new
    {
        fullName = "Ana Silva",
        cpf = RandomCpf(),
        birthDate = "1990-01-01",
        email = "ana@example.com",
        phone = "11999999999",
        address = new
        {
            zipCode = "01000-000",
            street = "Rua A",
            number = "123",
            neighborhood = "Centro",
            city = "São Paulo",
            state = "SP",
        },
        motherName = "Maria Silva",
        maritalStatus = "single",
        documentType = "CNH",
        documentNumber = "AB123456",
        issuingAuthority = "DETRAN",
        issuingState = "SP",
        issueDate = "2015-01-01",
        mockOutcome,
        expectedVersion,
    };

    [Fact]
    public async Task PutIdentification_WithValidPayload_PersistsDataAndQueryResult()
    {
        var leadId = await CreateLeadAsync();

        var response = await _client.PutAsJsonAsync($"/leads/{leadId}/steps/identification", ValidIdentificationBody());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal("Ana Silva", dto!.Identification!.FullName);
        Assert.Equal("CNH", dto.Identification.DocumentType);
        Assert.Equal("found", dto.Identification.Query!.Outcome);
    }

    [Theory]
    [InlineData("not_found")]
    [InlineData("diverging")]
    [InlineData("unavailable")]
    public async Task PutIdentification_WhenQueryOutcomeIsNotFound_PersistsResultWithoutWipingTypedData(string mockOutcome)
    {
        var leadId = await CreateLeadAsync();

        var response = await _client.PutAsJsonAsync($"/leads/{leadId}/steps/identification", ValidIdentificationBody(mockOutcome));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal(mockOutcome, dto!.Identification!.Query!.Outcome);
        Assert.Equal("Ana Silva", dto.Identification.FullName);
        Assert.Equal("CNH", dto.Identification.DocumentType);
        Assert.Equal("AB123456", dto.Identification.DocumentNumber);
    }

    [Fact]
    public async Task PutIdentification_WhenLeadDoesNotExist_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/leads/{Guid.NewGuid()}/steps/identification", ValidIdentificationBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutIdentification_WhenExpectedVersionMismatches_Returns409WithCurrentVersion()
    {
        var leadId = await CreateLeadAsync();

        var response = await _client.PutAsJsonAsync($"/leads/{leadId}/steps/identification", ValidIdentificationBody(expectedVersion: 999));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problemJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(problemJson);
        Assert.Equal(1, doc.RootElement.GetProperty("extensions").GetProperty("currentVersion").GetInt32());
    }

    [Fact]
    public async Task PutIdentification_WhenTestEndpointsFlagOnButNoMockOutcome_UsesDeterministicFoundDefault()
    {
        var leadId = await CreateLeadAsync();

        var response = await _client.PutAsJsonAsync($"/leads/{leadId}/steps/identification", ValidIdentificationBody());

        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal("found", dto!.Identification!.Query!.Outcome);
    }

    [Fact]
    public async Task PutIdentification_WhenTestEndpointsFlagOff_IgnoresMockOutcomeAndUsesFoundDefault()
    {
        var leadId = await CreateLeadAsync();

        // Forces a fresh host build (own env-var snapshot) with the flag off; assembly-level
        // parallelization is disabled (AssemblyInfo.cs) so this process-wide mutation is safe.
        Environment.SetEnvironmentVariable("ENABLE_TEST_ENDPOINTS", "false");
        try
        {
            await using var factoryWithFlagOff = _factory.WithWebHostBuilder(_ => { });
            using var client = factoryWithFlagOff.CreateClient();

            var response = await client.PutAsJsonAsync($"/leads/{leadId}/steps/identification", ValidIdentificationBody("unavailable"));

            var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
            Assert.Equal("found", dto!.Identification!.Query!.Outcome);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ENABLE_TEST_ENDPOINTS", "true");
        }
    }
}
