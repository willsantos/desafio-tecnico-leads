namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>Thrown when the mocked main system returns <c>unavailable</c>. Maps to 503.</summary>
public class MainSystemUnavailableException() : Exception("O sistema principal está indisponível.");
