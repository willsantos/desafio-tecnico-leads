using ConsignadoLeads.Api.Core.Models;

namespace ConsignadoLeads.Api.Features.Confirmation;

/// <summary>
/// Pure function validating etapa 6's pre-conditions (spec P1-6 AC1). No Mongo dependency —
/// takes the lead and its active documents directly, unit-testable in isolation.
/// </summary>
public static class PendingRequirementsValidator
{
    public const string PersonalDocumentType = "personal_document";
    public const string PayslipType = "payslip";

    public static IReadOnlyList<string> Validate(Lead lead, IReadOnlyList<LeadDocumentEntity> activeDocuments)
    {
        var reasons = new List<string>();

        if (!lead.Simulations.Any(s => s.Selected))
        {
            reasons.Add("Nenhuma simulação selecionada.");
        }

        if (lead.Identification?.Query is null)
        {
            reasons.Add("Consulta de identificação não foi concluída.");
        }

        var personalDocument = activeDocuments.FirstOrDefault(d => d.Type == PersonalDocumentType);
        var payslip = activeDocuments.FirstOrDefault(d => d.Type == PayslipType);

        if (personalDocument is null)
        {
            reasons.Add("Documento pessoal obrigatório não foi enviado.");
        }

        if (payslip is null)
        {
            reasons.Add("Contracheque obrigatório não foi enviado.");
        }

        if (personalDocument is not null && lead.Identification is not null
            && !string.Equals(personalDocument.PersonalDocumentSubtype, lead.Identification.DocumentType, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Documento pessoal enviado é incompatível com o tipo informado na etapa 3.");
        }

        return reasons;
    }
}
