namespace ConsignadoLeads.Api.Features.ProfessionalBankingData;

public record ProfessionalDataRequest(
    string EmploymentType,
    string Company,
    string RegistrationNumber,
    string Role,
    decimal MonthlyIncome,
    string AdmissionDate);

/// <summary><c>PixKey</c> is nullable/optional (spec P1-4 AC4) — every other field is required.</summary>
public record BankingDataRequest(
    string Bank,
    string Agency,
    string Account,
    string AccountDigit,
    string AccountType,
    string AccountHolder,
    string? PixKey);

public record ProfessionalBankingDataRequest(
    ProfessionalDataRequest ProfessionalData,
    BankingDataRequest BankingData,
    int? ExpectedVersion);
