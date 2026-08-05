using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MongoDb;

namespace ConsignadoLeads.Api.Tests.Integration;

/// <summary>
/// Shared fixture for every slice's integration tests: starts a real MongoDB container
/// (Testcontainers) and boots the API in-process against it via <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// One instance per test class (via <c>IClassFixture</c>) — each class gets its own isolated
/// database, so tests across different slices never interfere with each other.
///
/// Configuration is passed via process environment variables (<c>ConnectionStrings__MongoDb</c>,
/// <c>ENABLE_TEST_ENDPOINTS</c>) — the same mechanism <c>docker-compose.yml</c> uses in production
/// — because <c>Program.cs</c> reads <c>builder.Configuration</c> before <c>Build()</c>, at a point
/// where <see cref="WebApplicationFactory{TEntryPoint}.ConfigureWebHost"/>'s config overrides are
/// not yet visible for the minimal-hosting model. Test collection parallelization is disabled
/// assembly-wide (see AssemblyInfo.cs) so concurrent classes never race on these process-global vars.
/// </summary>
public class LeadRecoveryWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:7").Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _mongo.StartAsync();

        // Testcontainers' connection string already carries credentials + query params
        // (e.g. mongodb://mongo:mongo@host:port/?directConnection=true); insert the database
        // name right before the query string instead of naively appending it (which would
        // corrupt the last query value). Without an explicit database segment, the driver
        // defaults authSource to "admin" (where the container's mongo/mongo user actually
        // lives); adding a default database without also pinning authSource=admin would
        // silently break authentication, so it's added explicitly here.
        var raw = _mongo.GetConnectionString();
        var queryIndex = raw.IndexOf('?');
        var connectionString = queryIndex >= 0
            ? $"{raw[..queryIndex].TrimEnd('/')}/consignado_leads_test{raw[queryIndex..]}&authSource=admin"
            : $"{raw.TrimEnd('/')}/consignado_leads_test?authSource=admin";

        Environment.SetEnvironmentVariable("ConnectionStrings__MongoDb", connectionString);
        Environment.SetEnvironmentVariable("ENABLE_TEST_ENDPOINTS", "true");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _mongo.DisposeAsync();
        await base.DisposeAsync();
    }
}
