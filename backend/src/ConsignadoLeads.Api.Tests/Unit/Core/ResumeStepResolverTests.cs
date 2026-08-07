using ConsignadoLeads.Api.Core;

namespace ConsignadoLeads.Api.Tests.Unit.Core;

/// <summary>
/// Pure-logic tests for <see cref="ResumeStepResolver"/>. Each test maps to a spec acceptance
/// criterion (<c>.specs/features/progress-resume-step/spec.md</c>) or listed edge case — the
/// asserted value is the spec-defined <c>resumeStep</c>, not a mirror of the implementation.
/// </summary>
[Trait("Category", "Unit")]
public class ResumeStepResolverTests
{
    // PRS-02: confirmation-reached statuses resume on "confirmation".
    [Theory]
    [InlineData("pending_confirmation")]
    [InlineData("confirming")]
    [InlineData("pending_verification")]
    [InlineData("failed_retryable")]
    [InlineData("completed")]
    public void Resolve_ConfirmationReachedStatus_ReturnsConfirmation(string status)
    {
        var result = ResumeStepResolver.Resolve(
            status,
            ["consultation"],
            activeDocumentTypes: null);

        Assert.Equal("confirmation", result);
    }

    // PRS-03: first backend step missing from completedSteps is the resume step.
    public static IEnumerable<object[]> FirstMissingCases => new[]
    {
        new object[] { new[] { "consultation" }, "simulation" },
        new object[] { new[] { "consultation", "simulation" }, "identification" },
        new object[] { new[] { "consultation", "simulation", "identification" }, "professional-banking-data" },
    };

    [Theory]
    [MemberData(nameof(FirstMissingCases))]
    public void Resolve_FirstMissingBackendStep_ReturnsThatStep(string[] completed, string expected)
    {
        var result = ResumeStepResolver.Resolve("in_progress", completed, activeDocumentTypes: null);

        Assert.Equal(expected, result);
    }

    // PRS-04: all backend steps done, docs incomplete -> "documents".
    [Fact]
    public void Resolve_AllBackendDone_NoDocuments_ReturnsDocuments()
    {
        var allBackend = new[] { "consultation", "simulation", "identification", "professional-banking-data" };

        var result = ResumeStepResolver.Resolve("in_progress", allBackend, activeDocumentTypes: []);

        Assert.Equal("documents", result);
    }

    // PRS-05: all backend steps done + both doc types -> "confirmation".
    [Fact]
    public void Resolve_AllBackendDone_BothDocuments_ReturnsConfirmation()
    {
        var allBackend = new[] { "consultation", "simulation", "identification", "professional-banking-data" };

        var result = ResumeStepResolver.Resolve(
            "in_progress",
            allBackend,
            ["personal_document", "payslip"]);

        Assert.Equal("confirmation", result);
    }

    // PRS-06: empty/missing completedSteps -> "consultation" (defensive).
    [Fact]
    public void Resolve_EmptyCompletedSteps_ReturnsConsultation()
    {
        var result = ResumeStepResolver.Resolve("in_progress", [], activeDocumentTypes: null);

        Assert.Equal("consultation", result);
    }

    [Fact]
    public void Resolve_NullCompletedSteps_ReturnsConsultation()
    {
        var result = ResumeStepResolver.Resolve("in_progress", completedSteps: null!, activeDocumentTypes: null);

        Assert.Equal("consultation", result);
    }

    // Edge: only one of the two required documents -> still "documents".
    public static IEnumerable<object[]> PartialDocumentCases => new[]
    {
        new object[] { new[] { "personal_document" } },
        new object[] { new[] { "payslip" } },
    };

    [Theory]
    [MemberData(nameof(PartialDocumentCases))]
    public void Resolve_AllBackendDone_PartialDocuments_ReturnsDocuments(string[] docs)
    {
        var allBackend = new[] { "consultation", "simulation", "identification", "professional-banking-data" };

        var result = ResumeStepResolver.Resolve("in_progress", allBackend, docs);

        Assert.Equal("documents", result);
    }

    // Edge: unknown values in completedSteps are ignored when finding the first missing step.
    [Fact]
    public void Resolve_UnknownCompletedSteps_AreIgnored()
    {
        var result = ResumeStepResolver.Resolve(
            "in_progress",
            ["consultation", "bogus", "simulation", "identification"],
            activeDocumentTypes: null);

        Assert.Equal("professional-banking-data", result);
    }

    // Edge: status=abandoned computes via the general rule (no special-casing).
    [Fact]
    public void Resolve_AbandonedStatus_ComputesViaGeneralRule()
    {
        var result = ResumeStepResolver.Resolve(
            "abandoned",
            ["consultation", "simulation"],
            activeDocumentTypes: null);

        Assert.Equal("identification", result);
    }
}
