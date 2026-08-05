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
/// Required fields are non-nullable so ASP.NET Core's JSON body binding rejects a payload
/// missing any of them with a 400 automatically, matching the README's required-field list
/// for this route without a hand-rolled validator (mirrors the Simulation slice's approach).
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
