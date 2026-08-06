namespace ConsignadoLeads.Api.Features.Identification;

public record AddressRequest(
    string ZipCode,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State);

/// <summary>
/// Request body for the identification step. Field-presence is enforced by
/// <see cref="IdentificationValidator"/>, not by JSON binding alone.
/// </summary>
public record IdentificationRequest(
    string FullName,
    string Cpf,
    string BirthDate,
    string Email,
    string Phone,
    AddressRequest Address,
    string MotherName,
    string MaritalStatus,
    string DocumentType,
    string DocumentNumber,
    string IssuingAuthority,
    string IssuingState,
    string IssueDate,
    string? MockOutcome,
    int? ExpectedVersion);

/// <summary>Values accepted by the mocked identification check (README seção 5 mockOutcome table).</summary>
public enum IdentificationOutcome
{
    found,
    not_found,
    diverging,
    unavailable,
}
