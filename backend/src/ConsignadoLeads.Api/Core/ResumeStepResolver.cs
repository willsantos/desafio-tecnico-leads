namespace ConsignadoLeads.Api.Core;

/// <summary>
/// Derives <c>progress.resumeStep</c> — the next actionable wizard step for a lead — from state
/// the backend already owns: <see cref="Lead.Status"/>, <c>progress.completedSteps</c>, and the
/// lead's active document types. Pure function; never reads or writes Mongo. Called at DTO
/// projection time in <c>LeadMapper</c>, so the field is always fresh and needs no persistence
/// or migration (spec PRS-01..08, <c>.specs/features/progress-resume-step/spec.md</c>).
/// </summary>
public static class ResumeStepResolver
{
    /// <summary>Backend-tracked steps in flow order. <c>documents</c>/<c>confirmation</c> have no
    /// <c>completedSteps</c> marker and are reached only after these four are done.</summary>
    private static readonly string[] BackendStepOrder =
        ["consultation", "simulation", "identification", "professional-banking-data"];

    /// <summary>Statuses that mean etapa 6 has been (or is being) reached — resume lands on
    /// <c>confirmation</c> regardless of step progress. Mirrors the frontend's prior
    /// <c>CONFIRMATION_REACHED_STATUSES</c> set (<c>leadContext.tsx</c>).</summary>
    private static readonly HashSet<string> ConfirmationReachedStatuses = new()
    {
        "pending_confirmation",
        "confirming",
        "pending_verification",
        "failed_retryable",
        "completed",
    };

    /// <param name="activeDocumentTypes">Types of the lead's active (non-deleted, non-replaced)
    /// documents. Pass <c>null</c>/<c>empty</c> when unknown — the resolver then conservatively
    /// reports <c>documents</c> once all backend steps are done (the next actionable step).</param>
    public static string Resolve(
        string status,
        IReadOnlyList<string> completedSteps,
        IReadOnlyList<string>? activeDocumentTypes)
    {
        if (ConfirmationReachedStatuses.Contains(status))
        {
            return "confirmation";
        }

        var completed = completedSteps ?? [];
        foreach (var step in BackendStepOrder)
        {
            if (!completed.Contains(step))
            {
                return step;
            }
        }

        var docTypes = activeDocumentTypes ?? [];
        return docTypes.Contains("personal_document") && docTypes.Contains("payslip")
            ? "confirmation"
            : "documents";
    }
}
