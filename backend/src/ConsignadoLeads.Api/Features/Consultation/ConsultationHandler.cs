using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Core.Models;
using MongoDB.Driver;

namespace ConsignadoLeads.Api.Features.Consultation;

public class ConsultationHandler(MongoContext mongo, MockOutcomeResolver mockOutcomeResolver)
{
    public async Task<LeadDto> CreateAsync(ConsultationRequest request)
    {
        var now = DateTime.UtcNow;
        var outcome = mockOutcomeResolver.Resolve(request.MockOutcome, EligibilityOutcome.eligible);

        var lead = new Lead
        {
            Id = Guid.NewGuid().ToString(),
            Status = "in_progress",
            Version = 1,
            Progress = new Progress
            {
                CurrentStep = "consultation",
                StartedSteps = ["consultation"],
                CompletedSteps = ["consultation"],
                PendingItems = [],
                LastUpdatedAt = now,
            },
            Consultation = new Core.Models.Consultation
            {
                Input = new ConsultationInput
                {
                    Cpf = request.Cpf!,
                    BirthDate = request.BirthDate!,
                    BenefitType = request.BenefitType!,
                    BenefitNumber = request.BenefitNumber!,
                    PayingInstitution = request.PayingInstitution!,
                },
                Result = new ConsultationResult
                {
                    Outcome = outcome.ToString(),
                    AvailableMargin = EligibilityMock.AvailableMargin(outcome),
                    CheckedAt = now,
                },
            },
            CreatedAt = now,
            UpdatedAt = now,
        };

        await mongo.Leads.InsertOneAsync(lead);

        return LeadMapper.ToDto(lead);
    }

    public async Task<LeadDto> UpdateAsync(string id, ConsultationRequest request)
    {
        var now = DateTime.UtcNow;
        var outcome = mockOutcomeResolver.Resolve(request.MockOutcome, EligibilityOutcome.eligible);

        var update = Builders<Lead>.Update
            .Set(l => l.Consultation, new Core.Models.Consultation
            {
                Input = new ConsultationInput
                {
                    Cpf = request.Cpf!,
                    BirthDate = request.BirthDate!,
                    BenefitType = request.BenefitType!,
                    BenefitNumber = request.BenefitNumber!,
                    PayingInstitution = request.PayingInstitution!,
                },
                Result = new ConsultationResult
                {
                    Outcome = outcome.ToString(),
                    AvailableMargin = EligibilityMock.AvailableMargin(outcome),
                    CheckedAt = now,
                },
            })
            .Set(l => l.UpdatedAt, now)
            .Set(l => l.Progress.LastUpdatedAt, now)
            .Inc(l => l.Version, 1);

        var updated = await mongo.UpdateWithVersionCheckAsync(id, request.ExpectedVersion, update);
        return LeadMapper.ToDto(updated);
    }
}
