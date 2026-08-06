using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Core.Exceptions;
using ConsignadoLeads.Api.Core.Models;
using MongoDB.Driver;

namespace ConsignadoLeads.Api.Features.Confirmation;

public class ConfirmationHandler(MongoContext mongo, MockOutcomeResolver mockOutcomeResolver, IMainSystemMock mainSystemMock)
{
    private static readonly string[] MutexExcludedStatuses = ["confirming", "completed"];

    public async Task<ConfirmationResult> ConfirmAsync(string id, string? mockOutcome)
    {
        // The mutex acquisition and the active-documents lookup are independent (the latter
        // only needs `id`) — run them concurrently instead of serially.
        var mutexTask = AcquireMutexAsync(id);
        var documentsTask = mongo.GetActiveDocumentsAsync(id);
        await Task.WhenAll(mutexTask, documentsTask);
        var priorLead = await mutexTask;
        var activeDocuments = await documentsTask;

        var reasons = PendingRequirementsValidator.Validate(priorLead, activeDocuments);
        if (reasons.Count > 0)
        {
            // No mock call is made (spec P1-6 AC2). The mutex must still be released — the
            // lead goes back to whatever status it had before this attempt, not "confirming".
            await mongo.Leads.UpdateOneAsync(l => l.Id == id, Builders<Lead>.Update.Set(l => l.Status, priorLead.Status));
            throw new PendingRequirementsException(reasons);
        }

        var outcome = mockOutcomeResolver.Resolve(mockOutcome, MainSystemOutcome.success);
        return await ApplyOutcomeAsync(id, outcome);
    }

    public async Task<ConfirmationResult> RetrySubmissionAsync(string id)
    {
        var lead = await mongo.GetLeadOrThrowAsync(id);

        if (lead.Confirmation.FinalRegistration is not null && !string.IsNullOrEmpty(lead.Confirmation.FinalRegistration.RegistrationId))
        {
            // Idempotent short-circuit (spec P1-6 AC11) — no mutex needed since nothing mutates,
            // and no second call to the main system mock.
            return new ConfirmationResult(200, LeadMapper.ToDto(lead));
        }

        if (lead.Confirmation.Attempts.Count == 0)
        {
            throw new NoPriorConfirmationAttemptException(id);
        }

        await AcquireMutexAsync(id);

        // retry-submission takes no body (README seção 5) — always resolves to the
        // deterministic default, following the same outcome rules as confirm (spec P1-6 AC12).
        var outcome = mockOutcomeResolver.Resolve(null, MainSystemOutcome.success);
        return await ApplyOutcomeAsync(id, outcome);
    }

    /// <summary>
    /// Atomically flips the lead to "confirming" only if it isn't already confirming/completed
    /// (spec P1-6 AC9/AC14 — same mutex mechanism for confirm and retry-submission). Under true
    /// concurrency, MongoDB's findOneAndUpdate guarantees only one caller observes a non-null
    /// result; the other falls through to the disambiguation below.
    /// </summary>
    private async Task<Lead> AcquireMutexAsync(string id)
    {
        var filter = Builders<Lead>.Filter.And(
            Builders<Lead>.Filter.Eq(l => l.Id, id),
            Builders<Lead>.Filter.Nin(l => l.Status, MutexExcludedStatuses));
        var update = Builders<Lead>.Update.Set(l => l.Status, "confirming");
        var options = new FindOneAndUpdateOptions<Lead> { ReturnDocument = ReturnDocument.Before };

        var priorLead = await mongo.Leads.FindOneAndUpdateAsync(filter, update, options);
        if (priorLead is not null)
        {
            return priorLead;
        }

        var existing = await mongo.Leads.Find(l => l.Id == id).FirstOrDefaultAsync();
        if (existing is null)
        {
            throw new LeadNotFoundException(id);
        }

        if (existing.Status == "completed")
        {
            throw new LeadAlreadyCompletedException(id);
        }

        throw new ConfirmationInProgressException(id);
    }

    private async Task<ConfirmationResult> ApplyOutcomeAsync(string id, MainSystemOutcome outcome)
    {
        var now = DateTime.UtcNow;
        var attempt = new ConfirmationAttempt { AttemptedAt = now, Outcome = outcome.ToString() };
        var options = new FindOneAndUpdateOptions<Lead> { ReturnDocument = ReturnDocument.After };

        switch (outcome)
        {
            case MainSystemOutcome.success:
                {
                    var registration = new FinalRegistration { RegistrationId = mainSystemMock.GenerateRegistrationId(), CompletedAt = now };
                    var update = Builders<Lead>.Update
                        .Set(l => l.Status, "completed")
                        .Set(l => l.Confirmation.ConfirmedAt, now)
                        .Set(l => l.Confirmation.FinalRegistration, registration)
                        .Push(l => l.Confirmation.Attempts, attempt)
                        .Set(l => l.UpdatedAt, now)
                        .Inc(l => l.Version, 1);
                    var updated = await mongo.Leads.FindOneAndUpdateAsync(l => l.Id == id, update, options);
                    return new ConfirmationResult(200, LeadMapper.ToDto(updated!));
                }

            case MainSystemOutcome.indeterminate:
                {
                    var update = Builders<Lead>.Update
                        .Set(l => l.Status, "pending_verification")
                        .Push(l => l.Confirmation.Attempts, attempt)
                        .Set(l => l.UpdatedAt, now)
                        .Inc(l => l.Version, 1);
                    var updated = await mongo.Leads.FindOneAndUpdateAsync(l => l.Id == id, update, options);
                    return new ConfirmationResult(202, LeadMapper.ToDto(updated!));
                }

            case MainSystemOutcome.rejected:
                await SetFailedRetryableAsync(id, attempt, now);
                throw new PendingRequirementsException(["O sistema principal recusou o cadastro."]);

            case MainSystemOutcome.validationError:
                await SetFailedRetryableAsync(id, attempt, now);
                throw new PendingRequirementsException(["O sistema principal retornou erro de validação."]);

            case MainSystemOutcome.unavailable:
                await SetFailedRetryableAsync(id, attempt, now);
                throw new MainSystemUnavailableException();

            case MainSystemOutcome.timeout:
                await SetFailedRetryableAsync(id, attempt, now);
                throw new MainSystemTimeoutException();

            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }
    }

    private async Task SetFailedRetryableAsync(string id, ConfirmationAttempt attempt, DateTime now)
    {
        var update = Builders<Lead>.Update
            .Set(l => l.Status, "failed_retryable")
            .Push(l => l.Confirmation.Attempts, attempt)
            .Set(l => l.UpdatedAt, now)
            .Inc(l => l.Version, 1);
        await mongo.Leads.UpdateOneAsync(l => l.Id == id, update);
    }
}
