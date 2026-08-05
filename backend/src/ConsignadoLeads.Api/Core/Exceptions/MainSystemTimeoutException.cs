namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>Thrown when the mocked main system returns <c>timeout</c>. Maps to 504.</summary>
public class MainSystemTimeoutException() : Exception("O sistema principal excedeu o tempo limite.");
