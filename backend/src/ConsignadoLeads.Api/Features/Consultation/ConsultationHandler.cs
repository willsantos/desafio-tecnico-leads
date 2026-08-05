using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Core.Exceptions;
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

        var filterBuilder = Builders<Lead>.Filter;
        var filter = request.ExpectedVersion is int expectedVersion
            ? filterBuilder.And(filterBuilder.Eq(l => l.Id, id), filterBuilder.Eq(l => l.Version, expectedVersion))
            : filterBuilder.Eq(l => l.Id, id);

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

        var options = new FindOneAndUpdateOptions<Lead> { ReturnDocument = ReturnDocument.After };
        var updated = await mongo.Leads.FindOneAndUpdateAsync(filter, update, options);

        if (updated is not null)
        {
            return LeadMapper.ToDto(updated);
        }

        var existing = await mongo.Leads.Find(l => l.Id == id).FirstOrDefaultAsync();
        if (existing is null)
        {
            throw new LeadNotFoundException(id);
        }

        throw new VersionConflictException(request.ExpectedVersion!.Value, existing.Version);
    }
}
