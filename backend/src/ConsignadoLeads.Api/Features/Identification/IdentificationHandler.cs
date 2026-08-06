using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Core.Models;
using MongoDB.Driver;

namespace ConsignadoLeads.Api.Features.Identification;

public class IdentificationHandler(MongoContext mongo, MockOutcomeResolver mockOutcomeResolver)
{
    public async Task<LeadDto> UpdateAsync(string id, IdentificationRequest request)
    {
        var now = DateTime.UtcNow;
        var outcome = mockOutcomeResolver.Resolve(request.MockOutcome, IdentificationOutcome.found);

        var identification = new IdentificationData
        {
            FullName = request.FullName,
            Cpf = request.Cpf,
            BirthDate = request.BirthDate,
            Email = request.Email,
            Phone = request.Phone,
            Address = new Address
            {
                ZipCode = request.Address.ZipCode,
                Street = request.Address.Street,
                Number = request.Address.Number,
                Complement = request.Address.Complement,
                Neighborhood = request.Address.Neighborhood,
                City = request.Address.City,
                State = request.Address.State,
            },
            MotherName = request.MotherName,
            MaritalStatus = request.MaritalStatus,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber,
            IssuingAuthority = request.IssuingAuthority,
            IssuingState = request.IssuingState,
            IssueDate = request.IssueDate,
            // Persisted regardless of outcome (spec P1-3 AC2/AC3) — not-found/diverging/unavailable
            // never wipe the data just typed in, they only record the query result alongside it.
            Query = new IdentificationQuery { Outcome = IdentificationMock.Check(outcome) },
        };

        var update = Builders<Lead>.Update
            .Set(l => l.Identification, identification)
            .Set(l => l.Progress.CurrentStep, "identification")
            .AddToSet(l => l.Progress.StartedSteps, "identification")
            .AddToSet(l => l.Progress.CompletedSteps, "identification")
            .Set(l => l.Progress.LastUpdatedAt, now)
            .Set(l => l.UpdatedAt, now)
            .Inc(l => l.Version, 1);

        var updated = await mongo.UpdateWithVersionCheckAsync(id, request.ExpectedVersion, update);
        return LeadMapper.ToDto(updated);
    }
}
