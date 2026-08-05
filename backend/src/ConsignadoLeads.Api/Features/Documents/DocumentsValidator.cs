namespace ConsignadoLeads.Api.Features.Documents;

/// <summary>
/// Validates the upload request before any Mongo/GridFS call (design.md Error Handling
/// Strategy — size/content-type failures return 400 directly, not via an exception).
/// </summary>
public static class DocumentsValidator
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "application/pdf"];
    private static readonly string[] AllowedTypes = ["personal_document", "payslip"];

    public static IReadOnlyList<string> Validate(IFormFile? file, string? type, string? personalDocumentSubtype)
    {
        var errors = new List<string>();

        if (file is null)
        {
            errors.Add("file é obrigatório.");
        }
        else
        {
            if (file.Length > MaxSizeBytes)
            {
                errors.Add("Arquivo excede o limite de 10MB.");
            }

            if (!AllowedContentTypes.Contains(file.ContentType))
            {
                errors.Add("Tipo de conteúdo inválido. Use image/jpeg, image/png ou application/pdf.");
            }
        }

        if (string.IsNullOrWhiteSpace(type) || !AllowedTypes.Contains(type))
        {
            errors.Add("type é obrigatório e deve ser personal_document ou payslip.");
        }

        if (type == "personal_document" && string.IsNullOrWhiteSpace(personalDocumentSubtype))
        {
            errors.Add("personalDocumentSubtype é obrigatório quando type=personal_document.");
        }

        return errors;
    }
}
