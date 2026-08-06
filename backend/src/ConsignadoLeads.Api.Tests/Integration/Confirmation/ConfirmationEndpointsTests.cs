using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Features.Confirmation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ConsignadoLeads.Api.Tests.Integration.Confirmation;

/// <summary>Call-count spy substituted via DI to verify retry-submission's idempotency (spec P1-6 AC11).</summary>
public class SpyMainSystemMock : IMainSystemMock
{
    public int CallCount { get; private set; }

    public string GenerateRegistrationId()
    {
        CallCount++;
        return Guid.NewGuid().ToString();
    }
}

[Trait("Category", "Integration")]
[Collection(nameof(ConfirmationEndpointsTests))]
public class ConfirmationEndpointsTests : IClassFixture<LeadRecoveryWebApplicationFactory>
{
    private readonly LeadRecoveryWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ConfirmationEndpointsTests(LeadRecoveryWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static string RandomCpf() => Random.Shared.NextInt64(10_000_000_000, 99_999_999_999).ToString();

    private async Task<string> CreateLeadAsync(HttpClient? client = null)
    {
        client ??= _client;
        var body = new
        {
            cpf = RandomCpf(),
            birthDate = "1990-01-01",
            benefitType = "retirement",
            benefitNumber = "12345",
            payingInstitution = "INSS",
            consultationAuthorized = true,
        };

        var response = await client.PostAsJsonAsync("/leads/consultation", body);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        return dto!.Id;
    }

    /// <summary>Progresses a lead through etapas 1-5 so etapa 6's pendency validation passes.</summary>
    private async Task<string> CreateReadyToConfirmLeadAsync(HttpClient? client = null)
    {
        client ??= _client;
        var leadId = await CreateLeadAsync(client);

        await client.PostAsJsonAsync($"/leads/{leadId}/steps/simulation", new { requestedAmount = 10000m, installments = 24 });

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
        await client.PutAsJsonAsync($"/leads/{leadId}/steps/identification", identificationBody);

        using var personalForm = new MultipartFormDataContent();
        var personalFile = new ByteArrayContent([1, 2, 3]);
        personalFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        personalForm.Add(personalFile, "file", "cnh.jpg");
        personalForm.Add(new StringContent("personal_document"), "type");
        personalForm.Add(new StringContent("CNH"), "personalDocumentSubtype");
        await client.PostAsync($"/leads/{leadId}/documents", personalForm);

        using var payslipForm = new MultipartFormDataContent();
        var payslipFile = new ByteArrayContent([4, 5, 6]);
        payslipFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        payslipForm.Add(payslipFile, "file", "payslip.pdf");
        payslipForm.Add(new StringContent("payslip"), "type");
        await client.PostAsync($"/leads/{leadId}/documents", payslipForm);

        return leadId;
    }

    [Fact]
    public async Task Confirm_WhenPendenciesExist_Returns422WithoutCallingMock()
    {
        var spy = new SpyMainSystemMock();
        await using var spyFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IMainSystemMock>(spy)));
        using var client = spyFactory.CreateClient();

        var leadId = await CreateLeadAsync(client);

        var response = await client.PostAsync($"/leads/{leadId}/confirm", null);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        var problemJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(problemJson);
        var reasons = doc.RootElement.GetProperty("extensions").GetProperty("reasons").EnumerateArray().Select(r => r.GetString()).ToList();

        // A lead that only went through etapa 1 (consultation) is missing all 4 remaining
        // requirements — asserts the exact reasons list (spec P1-6 AC2), not a substring.
        Assert.Equal(
            [
                "Nenhuma simulação selecionada.",
                "Consulta de identificação não foi concluída.",
                "Documento pessoal obrigatório não foi enviado.",
                "Contracheque obrigatório não foi enviado.",
            ],
            reasons);
        Assert.Equal(0, spy.CallCount);
    }

    [Fact]
    public async Task Confirm_WhenMockOutcomeSuccess_Returns200CompletedWithRegistration()
    {
        var leadId = await CreateReadyToConfirmLeadAsync();

        var response = await _client.PostAsJsonAsync($"/leads/{leadId}/confirm", new { mockOutcome = "success" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal("completed", dto!.Status);
        Assert.NotNull(dto.Confirmation.FinalRegistration);
        Assert.NotEmpty(dto.Confirmation.FinalRegistration!.RegistrationId);
    }

    [Theory]
    [InlineData("rejected", 422, "failed_retryable")]
    [InlineData("validationError", 422, "failed_retryable")]
    [InlineData("unavailable", 503, "failed_retryable")]
    [InlineData("timeout", 504, "failed_retryable")]
    public async Task Confirm_WhenMockOutcomeFails_ReturnsDocumentedStatusAndPreservesLeadAsFailedRetryable(string mockOutcome, int expectedStatus, string expectedLeadStatus)
    {
        var leadId = await CreateReadyToConfirmLeadAsync();

        var response = await _client.PostAsJsonAsync($"/leads/{leadId}/confirm", new { mockOutcome });

        Assert.Equal(expectedStatus, (int)response.StatusCode);

        var leadResponse = await _client.GetAsync($"/leads/{leadId}");
        var lead = await leadResponse.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal(expectedLeadStatus, lead!.Status);
        Assert.NotNull(lead.Identification);
        Assert.Single(lead.Simulations);
    }

    [Fact]
    public async Task Confirm_WhenMockOutcomeIndeterminate_Returns202PendingVerification()
    {
        var leadId = await CreateReadyToConfirmLeadAsync();

        var response = await _client.PostAsJsonAsync($"/leads/{leadId}/confirm", new { mockOutcome = "indeterminate" });

        Assert.Equal((HttpStatusCode)202, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal("pending_verification", dto!.Status);
    }

    [Fact]
    public async Task Confirm_WhenAlreadyCompleted_Returns409()
    {
        var leadId = await CreateReadyToConfirmLeadAsync();
        await _client.PostAsJsonAsync($"/leads/{leadId}/confirm", new { mockOutcome = "success" });

        var response = await _client.PostAsJsonAsync($"/leads/{leadId}/confirm", new { mockOutcome = "success" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_WhenTestEndpointsFlagOff_UsesDeterministicSuccessDefault()
    {
        var leadId = await CreateReadyToConfirmLeadAsync();

        Environment.SetEnvironmentVariable("ENABLE_TEST_ENDPOINTS", "false");
        try
        {
            await using var factoryWithFlagOff = _factory.WithWebHostBuilder(_ => { });
            using var client = factoryWithFlagOff.CreateClient();

            var response = await client.PostAsJsonAsync($"/leads/{leadId}/confirm", new { mockOutcome = "rejected" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<LeadDto>();
            Assert.Equal("completed", dto!.Status);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ENABLE_TEST_ENDPOINTS", "true");
        }
    }

    [Fact]
    public async Task RetrySubmission_WhenNeverConfirmed_Returns409()
    {
        var leadId = await CreateReadyToConfirmLeadAsync();

        var response = await _client.PostAsync($"/leads/{leadId}/retry-submission", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RetrySubmission_WhenAlreadyRegistered_IsIdempotentAndDoesNotCallMockAgain()
    {
        var spy = new SpyMainSystemMock();
        await using var spyFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IMainSystemMock>(spy)));
        using var client = spyFactory.CreateClient();

        var leadId = await CreateReadyToConfirmLeadAsync(client);
        var confirmResponse = await client.PostAsJsonAsync($"/leads/{leadId}/confirm", new { mockOutcome = "success" });
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal(1, spy.CallCount);

        var retryResponse = await client.PostAsync($"/leads/{leadId}/retry-submission", null);

        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        var retried = await retryResponse.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal(confirmed!.Confirmation.FinalRegistration!.RegistrationId, retried!.Confirmation.FinalRegistration!.RegistrationId);
        Assert.Equal(1, spy.CallCount);
    }

    [Fact]
    public async Task RetrySubmission_WhenFailedRetryableWithoutRegistrationId_CallsMockAgainAndCanSucceed()
    {
        var spy = new SpyMainSystemMock();
        await using var spyFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IMainSystemMock>(spy)));
        using var client = spyFactory.CreateClient();

        var leadId = await CreateReadyToConfirmLeadAsync(client);
        await client.PostAsJsonAsync($"/leads/{leadId}/confirm", new { mockOutcome = "unavailable" });
        Assert.Equal(0, spy.CallCount);

        var retryResponse = await client.PostAsync($"/leads/{leadId}/retry-submission", null);

        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        var retried = await retryResponse.Content.ReadFromJsonAsync<LeadDto>();
        Assert.Equal("completed", retried!.Status);
        Assert.Equal(1, spy.CallCount);
    }

    [Fact]
    public async Task Confirm_TwoSimultaneousCalls_ExactlyOneSucceedsTheOtherGets409()
    {
        var leadId = await CreateReadyToConfirmLeadAsync();

        var firstTask = _client.PostAsJsonAsync($"/leads/{leadId}/confirm", new { mockOutcome = "success" });
        var secondTask = _client.PostAsJsonAsync($"/leads/{leadId}/confirm", new { mockOutcome = "success" });
        var responses = await Task.WhenAll(firstTask, secondTask);

        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        var processedCount = responses.Count(r => r.StatusCode != HttpStatusCode.Conflict);

        Assert.Equal(1, conflictCount);
        Assert.Equal(1, processedCount);
    }
}
