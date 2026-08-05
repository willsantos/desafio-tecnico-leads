namespace ConsignadoLeads.Api.Features.Consultation;

/// <summary>
/// Field-presence and format validation for <see cref="ConsultationRequest"/>, shared by the
/// create (POST) and re-run (PUT) endpoints. Pure function — no Mongo access.
/// </summary>
public static class ConsultationValidator
{
    private static readonly string[] ValidBenefitTypes = ["retirement", "pension", "public_servant", "clt"];

    public static IReadOnlyList<string> Validate(ConsultationRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Cpf) || !System.Text.RegularExpressions.Regex.IsMatch(request.Cpf, @"^\d{11}$"))
        {
            errors.Add("cpf é obrigatório e deve conter 11 dígitos.");
        }

        if (string.IsNullOrWhiteSpace(request.BirthDate))
        {
            errors.Add("birthDate é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.BenefitType) || !ValidBenefitTypes.Contains(request.BenefitType))
        {
            errors.Add("benefitType é obrigatório e deve ser um dos valores válidos.");
        }

        if (string.IsNullOrWhiteSpace(request.BenefitNumber))
        {
            errors.Add("benefitNumber é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.PayingInstitution))
        {
            errors.Add("payingInstitution é obrigatório.");
        }

        if (request.ConsultationAuthorized is not true)
        {
            errors.Add("consultationAuthorized deve ser true.");
        }

        return errors;
    }
}
