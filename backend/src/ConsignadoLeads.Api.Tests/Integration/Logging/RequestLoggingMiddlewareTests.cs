using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace ConsignadoLeads.Api.Tests.Integration.Logging;

/// <summary>Captures every log message written through it, for assertion in tests.</summary>
public class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<string> Messages { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void Dispose()
    {
    }

    private class CapturingLogger(CapturingLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            provider.Messages.Enqueue(formatter(state, exception));
    }
}

[Trait("Category", "Integration")]
[Collection(nameof(RequestLoggingMiddlewareTests))]
public class RequestLoggingMiddlewareTests(LeadRecoveryWebApplicationFactory factory) : IClassFixture<LeadRecoveryWebApplicationFactory>
{
    /// <summary>
    /// SensitiveDataMasker (T6) exists so RequestLoggingMiddleware's opt-in Debug-level body
    /// logging never writes a raw CPF, even when Debug logging is turned on for troubleshooting
    /// (spec P1-9 AC3). This proves the masking actually happens end-to-end through the real
    /// middleware pipeline, not just the pure-function unit tests in MaskerTests.
    /// </summary>
    [Fact]
    public async Task PostConsultation_WithDebugLoggingEnabled_LogsMaskedBodyWithoutRawCpf()
    {
        var capturingProvider = new CapturingLoggerProvider();
        await using var debugFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddProvider(capturingProvider);
            });
        });
        using var client = debugFactory.CreateClient();

        const string cpf = "12312312399";
        var body = new
        {
            cpf,
            birthDate = "1990-01-01",
            benefitType = "retirement",
            benefitNumber = "12345",
            payingInstitution = "INSS",
            consultationAuthorized = true,
        };

        await client.PostAsJsonAsync("/leads/consultation", body);

        var bodyLogEntry = capturingProvider.Messages.SingleOrDefault(m => m.Contains("request body", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(bodyLogEntry);
        Assert.Contains("REDACTED", bodyLogEntry);
        Assert.DoesNotContain(cpf, bodyLogEntry);
    }
}
