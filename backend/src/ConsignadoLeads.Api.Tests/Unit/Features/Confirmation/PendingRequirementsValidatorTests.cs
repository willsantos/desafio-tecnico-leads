using ConsignadoLeads.Api.Core.Models;
using ConsignadoLeads.Api.Features.Confirmation;

namespace ConsignadoLeads.Api.Tests.Unit.Features.Confirmation;

[Trait("Category", "Unit")]
public class PendingRequirementsValidatorTests
{
    private static Lead ValidLead() => new()
    {
        Id = "lead-1",
        Status = "in_progress",
        Version = 1,
        Simulations = [new ConsignadoLeads.Api.Core.Models.Simulation { Id = "sim-1", Selected = true }],
        Identification = new IdentificationData { DocumentType = "CNH", Query = new IdentificationQuery { Outcome = "found" } },
    };

    private static LeadDocumentEntity PersonalDocument(string subtype = "CNH") => new()
    {
        Id = "doc-personal",
        LeadId = "lead-1",
        Type = PendingRequirementsValidator.PersonalDocumentType,
        PersonalDocumentSubtype = subtype,
        Status = "uploaded",
    };

    private static LeadDocumentEntity Payslip() => new()
    {
        Id = "doc-payslip",
        LeadId = "lead-1",
        Type = PendingRequirementsValidator.PayslipType,
        Status = "uploaded",
    };

    [Fact]
    public void Validate_WhenAllRequirementsMet_ReturnsEmptyList()
    {
        var reasons = PendingRequirementsValidator.Validate(ValidLead(), [PersonalDocument(), Payslip()]);

        Assert.Empty(reasons);
    }

    [Fact]
    public void Validate_WhenNoSimulationSelected_ReturnsReason()
    {
        var lead = ValidLead();
        lead.Simulations = [new ConsignadoLeads.Api.Core.Models.Simulation { Id = "sim-1", Selected = false }];

        var reasons = PendingRequirementsValidator.Validate(lead, [PersonalDocument(), Payslip()]);

        Assert.Contains(reasons, r => r.Contains("simulação", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenIdentificationQueryMissing_ReturnsReason()
    {
        var lead = ValidLead();
        lead.Identification!.Query = null;

        var reasons = PendingRequirementsValidator.Validate(lead, [PersonalDocument(), Payslip()]);

        Assert.Contains(reasons, r => r.Contains("identificação", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenIdentificationAbsent_ReturnsReason()
    {
        var lead = ValidLead();
        lead.Identification = null;

        var reasons = PendingRequirementsValidator.Validate(lead, [PersonalDocument(), Payslip()]);

        Assert.Contains(reasons, r => r.Contains("identificação", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenPersonalDocumentMissing_ReturnsReason()
    {
        var reasons = PendingRequirementsValidator.Validate(ValidLead(), [Payslip()]);

        Assert.Contains(reasons, r => r.Contains("Documento pessoal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenPayslipMissing_ReturnsReason()
    {
        var reasons = PendingRequirementsValidator.Validate(ValidLead(), [PersonalDocument()]);

        Assert.Contains(reasons, r => r.Contains("Contracheque", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenPersonalDocumentSubtypeIncompatibleWithIdentification_ReturnsReason()
    {
        // etapa 3 informou CNH; documento enviado é RG (Cenário 8, RF13).
        var reasons = PendingRequirementsValidator.Validate(ValidLead(), [PersonalDocument(subtype: "RG"), Payslip()]);

        Assert.Contains(reasons, r => r.Contains("incompatível", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenEverythingMissing_ReturnsAllReasons()
    {
        var lead = new Lead { Id = "lead-1", Status = "in_progress", Version = 1 };

        var reasons = PendingRequirementsValidator.Validate(lead, []);

        Assert.Equal(4, reasons.Count);
    }
}
