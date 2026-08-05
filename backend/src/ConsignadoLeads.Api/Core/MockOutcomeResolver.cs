namespace ConsignadoLeads.Api.Core;

/// <summary>
/// Shared gate for the <c>mockOutcome</c>/<c>ENABLE_TEST_ENDPOINTS</c> pattern used by the
/// 3 mocked integration points (elegibilidade, identificação, sistema principal). Reads
/// <c>ENABLE_TEST_ENDPOINTS</c> from configuration once at construction time.
/// </summary>
public class MockOutcomeResolver
{
    private readonly bool _testEndpointsEnabled;

    public MockOutcomeResolver(IConfiguration configuration)
    {
        _testEndpointsEnabled = configuration.GetValue<bool>("ENABLE_TEST_ENDPOINTS");
    }

    /// <summary>
    /// Returns the parsed <paramref name="mockOutcome"/> when test endpoints are enabled and
    /// the value is a valid <typeparamref name="TEnum"/> member; otherwise returns
    /// <paramref name="deterministicDefault"/>.
    /// </summary>
    public TEnum Resolve<TEnum>(string? mockOutcome, TEnum deterministicDefault) where TEnum : struct, Enum
    {
        if (!_testEndpointsEnabled)
        {
            return deterministicDefault;
        }

        if (mockOutcome is not null && Enum.TryParse(mockOutcome, ignoreCase: true, out TEnum parsed))
        {
            return parsed;
        }

        return deterministicDefault;
    }
}
