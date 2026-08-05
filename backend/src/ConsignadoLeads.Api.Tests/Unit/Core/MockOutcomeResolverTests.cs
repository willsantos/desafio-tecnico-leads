using ConsignadoLeads.Api.Core;
using Microsoft.Extensions.Configuration;

namespace ConsignadoLeads.Api.Tests.Unit.Core;

[Trait("Category", "Unit")]
public class MockOutcomeResolverTests
{
    private enum TestOutcome
    {
        Eligible,
        NotEligible,
        Unavailable,
    }

    private static MockOutcomeResolver CreateResolver(string? enableTestEndpoints)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(enableTestEndpoints is null
                ? []
                : new Dictionary<string, string?> { ["ENABLE_TEST_ENDPOINTS"] = enableTestEndpoints })
            .Build();

        return new MockOutcomeResolver(configuration);
    }

    [Fact]
    public void Resolve_WhenFlagUnset_IgnoresMockOutcomeAndReturnsDefault()
    {
        var resolver = CreateResolver(enableTestEndpoints: null);

        var result = resolver.Resolve("Unavailable", TestOutcome.Eligible);

        Assert.Equal(TestOutcome.Eligible, result);
    }

    [Fact]
    public void Resolve_WhenFlagFalse_IgnoresMockOutcomeAndReturnsDefault()
    {
        var resolver = CreateResolver(enableTestEndpoints: "false");

        var result = resolver.Resolve("Unavailable", TestOutcome.Eligible);

        Assert.Equal(TestOutcome.Eligible, result);
    }

    [Fact]
    public void Resolve_WhenFlagTrueAndMockOutcomeValid_ReturnsParsedEnumValue()
    {
        var resolver = CreateResolver(enableTestEndpoints: "true");

        var result = resolver.Resolve("Unavailable", TestOutcome.Eligible);

        Assert.Equal(TestOutcome.Unavailable, result);
    }

    [Fact]
    public void Resolve_WhenFlagTrueAndMockOutcomeMissing_ReturnsDefault()
    {
        var resolver = CreateResolver(enableTestEndpoints: "true");

        var result = resolver.Resolve(null, TestOutcome.Eligible);

        Assert.Equal(TestOutcome.Eligible, result);
    }

    [Fact]
    public void Resolve_WhenFlagTrueAndMockOutcomeInvalid_ReturnsDefault()
    {
        var resolver = CreateResolver(enableTestEndpoints: "true");

        var result = resolver.Resolve("not-a-real-outcome", TestOutcome.Eligible);

        Assert.Equal(TestOutcome.Eligible, result);
    }
}
