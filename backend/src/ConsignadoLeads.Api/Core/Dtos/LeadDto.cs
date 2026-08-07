using ConsignadoLeads.Api.Core.Models;

namespace ConsignadoLeads.Api.Core.Dtos;

/// <summary>
/// API-boundary representation of a lead (README seção 5 "Shape de um lead"). Shared across
/// every slice that returns a lead (Consultation, Identification, ProfessionalBankingData,
/// Confirmation, Leads) so the contract shape is defined once instead of re-derived per
/// slice — the risk of 5 independently drifting copies of the same JSON contract outweighs
/// the slice-isolation trade-off described in design.md for this specific shape.
/// </summary>
public record LeadDto(
    string Id,
    string Status,
    int Version,
    ProgressDto Progress,
    ConsultationDto? Consultation,
    IReadOnlyList<SimulationDto> Simulations,
    IdentificationDataDto? Identification,
    ProfessionalDataDto? ProfessionalData,
    BankingDataDto? BankingData,
    ConfirmationDto Confirmation,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<DocumentDto>? Documents = null);

public record ProgressDto(
    string CurrentStep,
    IReadOnlyList<string> StartedSteps,
    IReadOnlyList<string> CompletedSteps,
    IReadOnlyList<string> PendingItems,
    DateTime LastUpdatedAt,
    string ResumeStep);

public record ConsultationDto(ConsultationInputDto Input, ConsultationResultDto? Result);

public record ConsultationInputDto(string Cpf, string BirthDate, string BenefitType, string BenefitNumber, string PayingInstitution);

public record ConsultationResultDto(string Outcome, decimal? AvailableMargin, DateTime CheckedAt);

public record SimulationDto(
    string Id,
    decimal RequestedAmount,
    int Installments,
    decimal InterestRate,
    decimal InstallmentAmount,
    decimal TotalAmount,
    bool Selected,
    DateTime SimulatedAt);

public record IdentificationDataDto(
    string FullName,
    string Cpf,
    string BirthDate,
    string Email,
    string Phone,
    AddressDto Address,
    string MotherName,
    string MaritalStatus,
    string DocumentType,
    string DocumentNumber,
    string IssuingAuthority,
    string IssuingState,
    string IssueDate,
    IdentificationQueryDto? Query);

public record AddressDto(string ZipCode, string Street, string Number, string? Complement, string Neighborhood, string City, string State);

public record IdentificationQueryDto(string Outcome);

public record ProfessionalDataDto(string EmploymentType, string Company, string RegistrationNumber, string Role, decimal MonthlyIncome, string AdmissionDate);

public record BankingDataDto(string Bank, string Agency, string Account, string AccountDigit, string AccountType, string AccountHolder, string? PixKey);

public record ConfirmationDto(DateTime? ConfirmedAt, IReadOnlyList<ConfirmationAttemptDto> Attempts, FinalRegistrationDto? FinalRegistration);

public record ConfirmationAttemptDto(DateTime AttemptedAt, string Outcome, string? Reason);

public record FinalRegistrationDto(string RegistrationId, DateTime CompletedAt);

public static class LeadMapper
{
    public static LeadDto ToDto(Lead lead, IReadOnlyList<DocumentDto>? documents = null) => new(
        lead.Id,
        lead.Status,
        lead.Version,
        MapProgress(lead.Status, lead.Progress, documents),
        MapConsultation(lead.Consultation),
        lead.Simulations.Select(MapSimulation).ToList(),
        MapIdentification(lead.Identification),
        MapProfessionalData(lead.ProfessionalData),
        MapBankingData(lead.BankingData),
        MapConfirmation(lead.Confirmation),
        lead.CreatedAt,
        lead.UpdatedAt,
        documents);

    private static ProgressDto MapProgress(string status, Progress progress, IReadOnlyList<DocumentDto>? documents)
    {
        var activeDocTypes = documents?
            .Where(d => d.Status == "uploaded")
            .Select(d => d.Type)
            .ToList();

        var resumeStep = ResumeStepResolver.Resolve(status, progress.CompletedSteps, activeDocTypes);

        return new ProgressDto(
            progress.CurrentStep,
            progress.StartedSteps,
            progress.CompletedSteps,
            progress.PendingItems,
            progress.LastUpdatedAt,
            resumeStep);
    }

    private static ConsultationDto? MapConsultation(Consultation? consultation)
    {
        if (consultation is null)
        {
            return null;
        }

        var input = new ConsultationInputDto(
            consultation.Input.Cpf,
            consultation.Input.BirthDate,
            consultation.Input.BenefitType,
            consultation.Input.BenefitNumber,
            consultation.Input.PayingInstitution);

        var result = consultation.Result is null
            ? null
            : new ConsultationResultDto(consultation.Result.Outcome, consultation.Result.AvailableMargin, consultation.Result.CheckedAt);

        return new ConsultationDto(input, result);
    }

    public static SimulationDto MapSimulation(Simulation simulation) => new(
        simulation.Id,
        simulation.RequestedAmount,
        simulation.Installments,
        simulation.InterestRate,
        simulation.InstallmentAmount,
        simulation.TotalAmount,
        simulation.Selected,
        simulation.SimulatedAt);

    private static IdentificationDataDto? MapIdentification(IdentificationData? identification)
    {
        if (identification is null)
        {
            return null;
        }

        var address = new AddressDto(
            identification.Address.ZipCode,
            identification.Address.Street,
            identification.Address.Number,
            identification.Address.Complement,
            identification.Address.Neighborhood,
            identification.Address.City,
            identification.Address.State);

        var query = identification.Query is null ? null : new IdentificationQueryDto(identification.Query.Outcome);

        return new IdentificationDataDto(
            identification.FullName,
            identification.Cpf,
            identification.BirthDate,
            identification.Email,
            identification.Phone,
            address,
            identification.MotherName,
            identification.MaritalStatus,
            identification.DocumentType,
            identification.DocumentNumber,
            identification.IssuingAuthority,
            identification.IssuingState,
            identification.IssueDate,
            query);
    }

    private static ProfessionalDataDto? MapProfessionalData(ProfessionalData? professionalData) =>
        professionalData is null
            ? null
            : new ProfessionalDataDto(
                professionalData.EmploymentType,
                professionalData.Company,
                professionalData.RegistrationNumber,
                professionalData.Role,
                professionalData.MonthlyIncome,
                professionalData.AdmissionDate);

    private static BankingDataDto? MapBankingData(BankingData? bankingData) =>
        bankingData is null
            ? null
            : new BankingDataDto(
                bankingData.Bank,
                bankingData.Agency,
                bankingData.Account,
                bankingData.AccountDigit,
                bankingData.AccountType,
                bankingData.AccountHolder,
                bankingData.PixKey);

    private static ConfirmationDto MapConfirmation(Confirmation confirmation)
    {
        var attempts = confirmation.Attempts.Select(a => new ConfirmationAttemptDto(a.AttemptedAt, a.Outcome, a.Reason)).ToList();
        var finalRegistration = confirmation.FinalRegistration is null
            ? null
            : new FinalRegistrationDto(confirmation.FinalRegistration.RegistrationId, confirmation.FinalRegistration.CompletedAt);

        return new ConfirmationDto(confirmation.ConfirmedAt, attempts, finalRegistration);
    }
}
