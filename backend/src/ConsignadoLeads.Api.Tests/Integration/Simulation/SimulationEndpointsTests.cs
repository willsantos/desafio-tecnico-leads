using System.Net;
using System.Net.Http.Json;
using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Core.Models;
using ConsignadoLeads.Api.Features.Simulation;
using Microsoft.Extensions.DependencyInjection;

namespace ConsignadoLeads.Api.Tests.Integration.Simulation;

[Trait("Category", "Integration")]
[Collection(nameof(SimulationEndpointsTests))]
public class SimulationEndpointsTests : IClassFixture<LeadRecoveryWebApplicationFactory>
{
    private readonly LeadRecoveryWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SimulationEndpointsTests(LeadRecoveryWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string RandomCpf() => Random.Shared.NextInt64(10_000_000_000, 99_999_999_999).ToString();

    private async Task<string> CreateLeadWithConsultationAsync()
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

    /// <summary>Inserts a lead directly in Mongo with no consultation, simulating a state
    /// unreachable through the public API — needed to exercise the 409 guard (AC6).</summary>
    private async Task<string> InsertLeadWithoutConsultationAsync()
    {
        var mongo = _factory.Services.GetRequiredService<MongoContext>();
        var id = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        await mongo.Leads.InsertOneAsync(new Lead
        {
            Id = id,
            Status = "in_progress",
            Version = 1,
            Progress = new Progress { CurrentStep = "consultation", LastUpdatedAt = now },
            Consultation = null,
            CreatedAt = now,
            UpdatedAt = now,
        });
        return id;
    }

    [Fact]
    public async Task PostSimulation_WithValidPayload_Returns201SelectedSimulation()
    {
        var leadId = await CreateLeadWithConsultationAsync();

        var response = await _client.PostAsJsonAsync($"/leads/{leadId}/steps/simulation", new { requestedAmount = 10000m, installments = 24 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<SimulationResponseDto>();
        Assert.Equal(516.81m, dto!.InstallmentAmount);
        Assert.Equal(12403.44m, dto.TotalAmount);
        Assert.True(dto.Selected);
    }

    [Fact]
    public async Task PostSimulation_WhenLeadDoesNotExist_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/leads/{Guid.NewGuid()}/steps/simulation", new { requestedAmount = 10000m, installments = 24 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostSimulation_WhenConsultationNotCompleted_Returns409()
    {
        var leadId = await InsertLeadWithoutConsultationAsync();

        var response = await _client.PostAsJsonAsync($"/leads/{leadId}/steps/simulation", new { requestedAmount = 10000m, installments = 24 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostSimulation_SecondSimulation_FlipsFirstToUnselectedButKeepsBothInHistory()
    {
        var leadId = await CreateLeadWithConsultationAsync();
        var firstResponse = await _client.PostAsJsonAsync($"/leads/{leadId}/steps/simulation", new { requestedAmount = 10000m, installments = 24 });
        var first = await firstResponse.Content.ReadFromJsonAsync<SimulationResponseDto>();

        var secondResponse = await _client.PostAsJsonAsync($"/leads/{leadId}/steps/simulation", new { requestedAmount = 5000m, installments = 12 });
        var second = await secondResponse.Content.ReadFromJsonAsync<SimulationResponseDto>();

        Assert.True(second!.Selected);

        var leadResponse = await _client.GetAsync($"/leads/{leadId}");
        var lead = await leadResponse.Content.ReadFromJsonAsync<LeadDto>();

        Assert.Equal(2, lead!.Simulations.Count);
        var firstInHistory = lead.Simulations.Single(s => s.Id == first!.Id);
        var secondInHistory = lead.Simulations.Single(s => s.Id == second.Id);
        Assert.False(firstInHistory.Selected);
        Assert.True(secondInHistory.Selected);
    }

    [Fact]
    public async Task PatchSelect_SwitchesSelectionWithoutRecalculating()
    {
        var leadId = await CreateLeadWithConsultationAsync();
        var firstResponse = await _client.PostAsJsonAsync($"/leads/{leadId}/steps/simulation", new { requestedAmount = 10000m, installments = 24 });
        var first = await firstResponse.Content.ReadFromJsonAsync<SimulationResponseDto>();
        await _client.PostAsJsonAsync($"/leads/{leadId}/steps/simulation", new { requestedAmount = 5000m, installments = 12 });

        var selectResponse = await _client.PatchAsync($"/leads/{leadId}/steps/simulation/{first!.Id}/select", null);

        Assert.Equal(HttpStatusCode.OK, selectResponse.StatusCode);
        var selected = await selectResponse.Content.ReadFromJsonAsync<SimulationResponseDto>();
        Assert.True(selected!.Selected);
        Assert.Equal(first.InstallmentAmount, selected.InstallmentAmount);
        Assert.Equal(first.TotalAmount, selected.TotalAmount);

        var leadResponse = await _client.GetAsync($"/leads/{leadId}");
        var lead = await leadResponse.Content.ReadFromJsonAsync<LeadDto>();
        Assert.True(lead!.Simulations.Single(s => s.Id == first.Id).Selected);
        Assert.Single(lead.Simulations, s => s.Selected);
    }

    [Fact]
    public async Task PatchSelect_WhenLeadDoesNotExist_Returns404()
    {
        var response = await _client.PatchAsync($"/leads/{Guid.NewGuid()}/steps/simulation/{Guid.NewGuid()}/select", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchSelect_WhenSimulationDoesNotExist_Returns404()
    {
        var leadId = await CreateLeadWithConsultationAsync();
        await _client.PostAsJsonAsync($"/leads/{leadId}/steps/simulation", new { requestedAmount = 10000m, installments = 24 });

        var response = await _client.PatchAsync($"/leads/{leadId}/steps/simulation/{Guid.NewGuid()}/select", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
