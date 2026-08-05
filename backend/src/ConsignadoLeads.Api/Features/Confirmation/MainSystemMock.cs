namespace ConsignadoLeads.Api.Features.Confirmation;

/// <summary>Values accepted by the mocked main system (README seção 5 mockOutcome table).</summary>
public enum MainSystemOutcome
{
    success,
    rejected,
    validationError,
    unavailable,
    timeout,
    indeterminate,
}

/// <summary>
/// Mocked main-system call (README seção 4.6). Behind an interface (not a static class, unlike
/// the other two mocks) so tests can substitute a call-counting spy to verify retry-submission's
/// idempotency (spec P1-6 AC11 — no second call when a registrationId already exists).
/// </summary>
public interface IMainSystemMock
{
    string GenerateRegistrationId();
}

public class MainSystemMock : IMainSystemMock
{
    public string GenerateRegistrationId() => Guid.NewGuid().ToString();
}
