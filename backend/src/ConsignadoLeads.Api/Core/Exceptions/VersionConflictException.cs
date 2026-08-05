namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Thrown when a request's <c>expectedVersion</c> does not match the lead's current
/// version. Maps to 409 with <c>extensions.currentVersion</c>.
/// </summary>
public class VersionConflictException(int expectedVersion, int currentVersion)
    : Exception($"A versão enviada ({expectedVersion}) não corresponde à versão atual do lead ({currentVersion}).")
{
    public int CurrentVersion { get; } = currentVersion;
}
