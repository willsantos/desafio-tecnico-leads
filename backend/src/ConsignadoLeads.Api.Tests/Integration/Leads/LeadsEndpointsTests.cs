using System.Net;
using System.Net.Http.Json;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Features.Leads;

namespace ConsignadoLeads.Api.Tests.Integration.Leads;

[Trait("Category", "Integration")]
[Collection(nameof(LeadsEndpointsTests))]
public class LeadsEndpointsTests : IClassFixture<LeadRecoveryWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LeadsEndpointsTests(LeadRecoveryWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<LeadDto> CreateLeadAsync(string cpf)
    {
        var body = new
        {
            cpf,
            birthDate = "1990-01-01",
            benefitType = "retirement",
            benefitNumber = "12345",
            payingInstitution = "INSS",
            consultationAuthorized = true,
        };

        var response = await _client.PostAsJsonAsync("/leads/consultation", body);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        return dto!;
    }

    /// <summary>Generates a syntactically valid (11-digit) unique CPF for test data.</summary>
    private static string RandomCpf() => Random.Shared.NextInt64(10_000_000_000, 99_999_999_999).ToString();

    [Fact]
    public async Task GetLeads_DefaultPageSize_Is20()
    {
        var response = await _client.GetAsync("/leads?cpf=nonexistent-cpf-marker");

        var result = await response.Content.ReadFromJsonAsync<PagedLeadsResponse>();
        Assert.Equal(20, result!.PageSize);
    }

    [Fact]
    public async Task GetLeads_PageSizeAbove100_ClampsAt100()
    {
        var response = await _client.GetAsync("/leads?cpf=nonexistent-cpf-marker&pageSize=500");

        var result = await response.Content.ReadFromJsonAsync<PagedLeadsResponse>();
        Assert.Equal(100, result!.PageSize);
    }

    [Fact]
    public async Task GetLeads_SortsByCreatedAtDescending()
    {
        var first = await CreateLeadAsync(RandomCpf());
        var second = await CreateLeadAsync(RandomCpf());
        var third = await CreateLeadAsync(RandomCpf());

        var response = await _client.GetAsync($"/leads?status=in_progress&pageSize=100");
        var result = await response.Content.ReadFromJsonAsync<PagedLeadsResponse>();

        var ids = result!.Items.Select(i => i.Id).ToList();
        var indexThird = ids.IndexOf(third.Id);
        var indexSecond = ids.IndexOf(second.Id);
        var indexFirst = ids.IndexOf(first.Id);

        Assert.True(indexThird < indexSecond);
        Assert.True(indexSecond < indexFirst);
    }

    [Fact]
    public async Task GetLeads_FiltersByCpf()
    {
        var cpf = RandomCpf();
        var created = await CreateLeadAsync(cpf);
        await CreateLeadAsync(RandomCpf());

        var response = await _client.GetAsync($"/leads?cpf={cpf}");
        var result = await response.Content.ReadFromJsonAsync<PagedLeadsResponse>();

        Assert.Equal(1, result!.TotalItems);
        var summary = result.Items.Single();
        Assert.Equal(created.Id, summary.Id);
        Assert.Equal("in_progress", summary.Status);
        Assert.Equal("consultation", summary.Progress.CurrentStep);
        Assert.NotEqual(default, summary.CreatedAt);
        Assert.NotEqual(default, summary.UpdatedAt);
        Assert.Null(summary.FinalRegistration);
    }

    [Fact]
    public async Task GetLeads_PaginatesWithPageSize()
    {
        await CreateLeadAsync(RandomCpf());
        await CreateLeadAsync(RandomCpf());
        await CreateLeadAsync(RandomCpf());

        // Isolation via a unique status marker instead of cpf (cpf is exact-match, not prefix)
        // is impractical here; page correctness for a bounded slice is verified using the
        // pageSize/page math directly against a status-scoped result set.
        var page1 = await _client.GetFromJsonAsync<PagedLeadsResponse>($"/leads?status=in_progress&page=1&pageSize=2");
        var page2 = await _client.GetFromJsonAsync<PagedLeadsResponse>($"/leads?status=in_progress&page=2&pageSize=2");

        Assert.Equal(2, page1!.Items.Count);
        Assert.True(page2!.Items.Count >= 1);
        Assert.Empty(page1.Items.Select(i => i.Id).Intersect(page2.Items.Select(i => i.Id)));
    }

    [Fact]
    public async Task GetLeadById_WhenExists_ReturnsFullLead()
    {
        var created = await CreateLeadAsync(RandomCpf());

        var response = await _client.GetAsync($"/leads/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal(created.Id, dto!.Id);
        Assert.Equal("in_progress", dto.Status);
    }

    [Fact]
    public async Task GetLeadById_WhenMissing_Returns404()
    {
        var response = await _client.GetAsync($"/leads/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
