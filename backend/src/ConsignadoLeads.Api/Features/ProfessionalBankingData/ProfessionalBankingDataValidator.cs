namespace ConsignadoLeads.Api.Features.ProfessionalBankingData;

/// <summary>
/// Field-presence validation for <see cref="ProfessionalBankingDataRequest"/>, shared by the
/// update endpoint. Pure function — no Mongo access.
/// </summary>
public static class ProfessionalBankingDataValidator
{
    public static IReadOnlyList<string> Validate(ProfessionalBankingDataRequest request)
    {
        var errors = new List<string>();

        if (request.ProfessionalData is null)
        {
            errors.Add("professionalData é obrigatório.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.ProfessionalData.EmploymentType))
            {
                errors.Add("professionalData.employmentType é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.ProfessionalData.Company))
            {
                errors.Add("professionalData.company é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.ProfessionalData.RegistrationNumber))
            {
                errors.Add("professionalData.registrationNumber é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.ProfessionalData.Role))
            {
                errors.Add("professionalData.role é obrigatório.");
            }

            if (request.ProfessionalData.MonthlyIncome <= 0)
            {
                errors.Add("professionalData.monthlyIncome deve ser maior que zero.");
            }

            if (string.IsNullOrWhiteSpace(request.ProfessionalData.AdmissionDate))
            {
                errors.Add("professionalData.admissionDate é obrigatório.");
            }
        }

        if (request.BankingData is null)
        {
            errors.Add("bankingData é obrigatório.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.BankingData.Bank))
            {
                errors.Add("bankingData.bank é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.BankingData.Agency))
            {
                errors.Add("bankingData.agency é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.BankingData.Account))
            {
                errors.Add("bankingData.account é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.BankingData.AccountDigit))
            {
                errors.Add("bankingData.accountDigit é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.BankingData.AccountType))
            {
                errors.Add("bankingData.accountType é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.BankingData.AccountHolder))
            {
                errors.Add("bankingData.accountHolder é obrigatório.");
            }
        }

        return errors;
    }
}
