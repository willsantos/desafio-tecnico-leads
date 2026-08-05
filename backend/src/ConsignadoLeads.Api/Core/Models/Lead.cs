using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ConsignadoLeads.Api.Core.Models;

/// <summary>
/// Persistence POCO for the "leads" collection. Never returned directly by an endpoint —
/// API boundaries expose DTOs mapped from this type (spec P1-8 AC6).
/// </summary>
public class Lead
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int Version { get; set; }

    public int SchemaVersion { get; set; } = 1;

    public Progress Progress { get; set; } = new();

    [BsonIgnoreIfNull]
    public Consultation? Consultation { get; set; }

    public List<Simulation> Simulations { get; set; } = new();

    [BsonIgnoreIfNull]
    public IdentificationData? Identification { get; set; }

    [BsonIgnoreIfNull]
    public ProfessionalData? ProfessionalData { get; set; }

    [BsonIgnoreIfNull]
    public BankingData? BankingData { get; set; }

    public Confirmation Confirmation { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class Progress
{
    public string CurrentStep { get; set; } = string.Empty;

    public List<string> StartedSteps { get; set; } = new();

    public List<string> CompletedSteps { get; set; } = new();

    public List<string> PendingItems { get; set; } = new();

    public DateTime LastUpdatedAt { get; set; }
}

public class Consultation
{
    public ConsultationInput Input { get; set; } = new();

    [BsonIgnoreIfNull]
    public ConsultationResult? Result { get; set; }
}

public class ConsultationInput
{
    public string Cpf { get; set; } = string.Empty;

    public string BirthDate { get; set; } = string.Empty;

    public string BenefitType { get; set; } = string.Empty;

    public string BenefitNumber { get; set; } = string.Empty;

    public string PayingInstitution { get; set; } = string.Empty;
}

public class ConsultationResult
{
    public string Outcome { get; set; } = string.Empty;

    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? AvailableMargin { get; set; }

    public DateTime CheckedAt { get; set; }
}

public class Simulation
{
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal RequestedAmount { get; set; }

    public int Installments { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal InterestRate { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal InstallmentAmount { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalAmount { get; set; }

    public bool Selected { get; set; }

    public DateTime SimulatedAt { get; set; }
}

public class IdentificationData
{
    public string FullName { get; set; } = string.Empty;

    public string Cpf { get; set; } = string.Empty;

    public string BirthDate { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public Address Address { get; set; } = new();

    public string MotherName { get; set; } = string.Empty;

    public string MaritalStatus { get; set; } = string.Empty;

    public string DocumentType { get; set; } = string.Empty;

    public string DocumentNumber { get; set; } = string.Empty;

    public string IssuingAuthority { get; set; } = string.Empty;

    public string IssuingState { get; set; } = string.Empty;

    public string IssueDate { get; set; } = string.Empty;

    [BsonIgnoreIfNull]
    public IdentificationQuery? Query { get; set; }
}

public class Address
{
    public string ZipCode { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    [BsonIgnoreIfNull]
    public string? Complement { get; set; }

    public string Neighborhood { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;
}

public class IdentificationQuery
{
    public string Outcome { get; set; } = string.Empty;
}

public class ProfessionalData
{
    public string EmploymentType { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string RegistrationNumber { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal MonthlyIncome { get; set; }

    public string AdmissionDate { get; set; } = string.Empty;
}

public class BankingData
{
    public string Bank { get; set; } = string.Empty;

    public string Agency { get; set; } = string.Empty;

    public string Account { get; set; } = string.Empty;

    public string AccountDigit { get; set; } = string.Empty;

    public string AccountType { get; set; } = string.Empty;

    public string AccountHolder { get; set; } = string.Empty;

    [BsonIgnoreIfNull]
    public string? PixKey { get; set; }
}

public class Confirmation
{
    [BsonIgnoreIfNull]
    public DateTime? ConfirmedAt { get; set; }

    public List<ConfirmationAttempt> Attempts { get; set; } = new();

    [BsonIgnoreIfNull]
    public FinalRegistration? FinalRegistration { get; set; }
}

public class ConfirmationAttempt
{
    public DateTime AttemptedAt { get; set; }

    public string Outcome { get; set; } = string.Empty;

    [BsonIgnoreIfNull]
    public string? Reason { get; set; }
}

public class FinalRegistration
{
    public string RegistrationId { get; set; } = string.Empty;

    public DateTime CompletedAt { get; set; }
}
