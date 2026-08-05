using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Core.Exceptions;
using ConsignadoLeads.Api.Core.Models;
using MongoDB.Driver;

namespace ConsignadoLeads.Api.Features.ProfessionalBankingData;

public class ProfessionalBankingDataHandler(MongoContext mongo)
{
    public async Task<LeadDto> UpdateAsync(string id, ProfessionalBankingDataRequest request)
    {
        var now = DateTime.UtcNow;

        var professionalData = new Core.Models.ProfessionalData
        {
            EmploymentType = request.ProfessionalData.EmploymentType,
            Company = request.ProfessionalData.Company,
            RegistrationNumber = request.ProfessionalData.RegistrationNumber,
            Role = request.ProfessionalData.Role,
            MonthlyIncome = request.ProfessionalData.MonthlyIncome,
            AdmissionDate = request.ProfessionalData.AdmissionDate,
        };

        var bankingData = new Core.Models.BankingData
        {
            Bank = request.BankingData.Bank,
            Agency = request.BankingData.Agency,
            Account = request.BankingData.Account,
            AccountDigit = request.BankingData.AccountDigit,
            AccountType = request.BankingData.AccountType,
            AccountHolder = request.BankingData.AccountHolder,
            PixKey = request.BankingData.PixKey,
        };

        var filterBuilder = Builders<Lead>.Filter;
        var filter = request.ExpectedVersion is int expectedVersion
            ? filterBuilder.And(filterBuilder.Eq(l => l.Id, id), filterBuilder.Eq(l => l.Version, expectedVersion))
            : filterBuilder.Eq(l => l.Id, id);

        // Sets only the professionalData/bankingData/progress/version/updatedAt fields —
        // never touches consultation/simulations/identification, which stay untouched on the
        // document (spec P1-4 AC1, P1-8 AC4).
        var update = Builders<Lead>.Update
            .Set(l => l.ProfessionalData, professionalData)
            .Set(l => l.BankingData, bankingData)
            .Set(l => l.Progress.CurrentStep, "professional-banking-data")
            .AddToSet(l => l.Progress.StartedSteps, "professional-banking-data")
            .AddToSet(l => l.Progress.CompletedSteps, "professional-banking-data")
            .Set(l => l.Progress.LastUpdatedAt, now)
            .Set(l => l.UpdatedAt, now)
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
