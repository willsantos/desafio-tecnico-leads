namespace ConsignadoLeads.Api.Features.Identification;

/// <summary>
/// Field-presence validation for <see cref="IdentificationRequest"/>, shared by the update
/// endpoint. Pure function — no Mongo access.
/// </summary>
public static class IdentificationValidator
{
    private static readonly string[] ValidDocumentTypes = ["CNH", "RG"];

    public static IReadOnlyList<string> Validate(IdentificationRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors.Add("fullName é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Cpf))
        {
            errors.Add("cpf é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.BirthDate))
        {
            errors.Add("birthDate é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("email é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            errors.Add("phone é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.MotherName))
        {
            errors.Add("motherName é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.MaritalStatus))
        {
            errors.Add("maritalStatus é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.DocumentType) || !ValidDocumentTypes.Contains(request.DocumentType))
        {
            errors.Add("documentType é obrigatório e deve ser CNH ou RG.");
        }

        if (string.IsNullOrWhiteSpace(request.DocumentNumber))
        {
            errors.Add("documentNumber é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.IssuingAuthority))
        {
            errors.Add("issuingAuthority é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.IssuingState))
        {
            errors.Add("issuingState é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.IssueDate))
        {
            errors.Add("issueDate é obrigatório.");
        }

        if (request.Address is null)
        {
            errors.Add("address é obrigatório.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Address.ZipCode))
            {
                errors.Add("address.zipCode é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.Address.Street))
            {
                errors.Add("address.street é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.Address.Number))
            {
                errors.Add("address.number é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.Address.Neighborhood))
            {
                errors.Add("address.neighborhood é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.Address.City))
            {
                errors.Add("address.city é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(request.Address.State))
            {
                errors.Add("address.state é obrigatório.");
            }
        }

        return errors;
    }
}
