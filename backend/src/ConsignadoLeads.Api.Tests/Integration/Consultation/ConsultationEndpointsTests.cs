using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace ConsignadoLeads.Api.Tests.Integration.Consultation;

[Trait("Category", "Integration")]
[Collection(nameof(ConsultationEndpointsTests))]
public class ConsultationEndpointsTests : IClassFixture<LeadRecoveryWebApplicationFactory>
{
    private readonly LeadRecoveryWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ConsultationEndpointsTests(LeadRecoveryWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>Counts leads directly in Mongo (bypasses HTTP — GET /leads is a different slice, T8).</summary>
    private async Task<long> CountLeadsByCpfAsync(string cpf)
    {
        var mongo = _factory.Services.GetRequiredService<MongoContext>();
        return await mongo.Leads.CountDocumentsAsync(Builders<Core.Models.Lead>.Filter.Eq("consultation.input.cpf", cpf));
    }

    private static object ValidRequestBody(string cpf, string? mockOutcome = null) => new
    {
        cpf,
        birthDate = "1990-01-01",
        benefitType = "retirement",
        benefitNumber = "12345",
        payingInstitution = "INSS",
        consultationAuthorized = true,
        mockOutcome,
    };

    [Fact]
    public async Task PostConsultation_WithValidPayload_Creates201LeadInProgress()
    {
        var response = await _client.PostAsJsonAsync("/leads/consultation", ValidRequestBody("11111111111"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.NotNull(dto);
        Assert.Equal("in_progress", dto!.Status);
        Assert.Equal(1, dto.Version);
        Assert.Equal("consultation", dto.Progress.CurrentStep);
    }

    [Fact]
    public async Task PostConsultation_WhenConsultationAuthorizedFalse_Returns400AndCreatesNoLead()
    {
        var body = new
        {
            cpf = "22222222222",
            birthDate = "1990-01-01",
            benefitType = "retirement",
            benefitNumber = "12345",
            payingInstitution = "INSS",
            consultationAuthorized = false,
        };

        var response = await _client.PostAsJsonAsync("/leads/consultation", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountLeadsByCpfAsync("22222222222"));
    }

    [Fact]
    public async Task PostConsultation_WhenRequiredFieldMissing_Returns400AndCreatesNoLead()
    {
        var body = new
        {
            cpf = "33333333333",
            birthDate = "1990-01-01",
            benefitType = "retirement",
            // benefitNumber missing
            payingInstitution = "INSS",
            consultationAuthorized = true,
        };

        var response = await _client.PostAsJsonAsync("/leads/consultation", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountLeadsByCpfAsync("33333333333"));
    }

    [Fact]
    public async Task PostConsultation_WhenMockOutcomeUnavailable_CreatesLeadWithUnavailableResult()
    {
        var response = await _client.PostAsJsonAsync("/leads/consultation", ValidRequestBody("44444444444", "unavailable"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.NotNull(dto);
        Assert.Equal("in_progress", dto!.Status);
        Assert.Equal("unavailable", dto.Consultation!.Result!.Outcome);
        Assert.Equal("44444444444", dto.Consultation.Input.Cpf);
    }

    [Fact]
    public async Task PutConsultation_OnExistingLead_ReRunsWithoutCreatingNewLead()
    {
        var createResponse = await _client.PostAsJsonAsync("/leads/consultation", ValidRequestBody("55555555555"));
        var created = await createResponse.Content.ReadFromJsonAsync<LeadDto>();

        var putResponse = await _client.PutAsJsonAsync($"/leads/{created!.Id}/steps/consultation", ValidRequestBody("55555555555", "not_eligible"));

        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var updated = await putResponse.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("not_eligible", updated.Consultation!.Result!.Outcome);
        Assert.Equal(1, await CountLeadsByCpfAsync("55555555555"));
    }

    [Fact]
    public async Task PutConsultation_WhenLeadDoesNotExist_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/leads/{Guid.NewGuid()}/steps/consultation", ValidRequestBody("66666666666"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutConsultation_WhenExpectedVersionMismatches_Returns409WithCurrentVersion()
    {
        var createResponse = await _client.PostAsJsonAsync("/leads/consultation", ValidRequestBody("77777777777"));
        var created = await createResponse.Content.ReadFromJsonAsync<LeadDto>();

        var body = new
        {
            cpf = "77777777777",
            birthDate = "1990-01-01",
            benefitType = "retirement",
            benefitNumber = "12345",
            payingInstitution = "INSS",
            consultationAuthorized = true,
            expectedVersion = 999,
        };

        var response = await _client.PutAsJsonAsync($"/leads/{created!.Id}/steps/consultation", body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problemJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(problemJson);
        Assert.Equal(1, doc.RootElement.GetProperty("extensions").GetProperty("currentVersion").GetInt32());
    }

    [Fact]
    public async Task PostConsultation_WhenTestEndpointsFlagOnButNoMockOutcome_UsesDeterministicEligibleDefault()
    {
        var response = await _client.PostAsJsonAsync("/leads/consultation", ValidRequestBody("88888888888"));

        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal("eligible", dto!.Consultation!.Result!.Outcome);
    }

    [Fact]
    public async Task PostConsultation_WhenTestEndpointsFlagOff_IgnoresMockOutcomeAndUsesEligibleDefault()
    {
        // Forces a fresh host build (own env-var snapshot) with the flag off; assembly-level
        // parallelization is disabled (AssemblyInfo.cs) so this process-wide mutation is safe.
        Environment.SetEnvironmentVariable("ENABLE_TEST_ENDPOINTS", "false");
        try
        {
            await using var factoryWithFlagOff = _factory.WithWebHostBuilder(_ => { });
            using var client = factoryWithFlagOff.CreateClient();

            var response = await client.PostAsJsonAsync("/leads/consultation", ValidRequestBody("99999999999", "unavailable"));

            var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
            Assert.Equal("eligible", dto!.Consultation!.Result!.Outcome);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ENABLE_TEST_ENDPOINTS", "true");
        }
    }
}
