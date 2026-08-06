using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;
using ConsignadoLeads.Api.Core.Exceptions;
using ConsignadoLeads.Api.Core.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ConsignadoLeads.Api.Features.Simulation;

public class SimulationHandler(MongoContext mongo)
{
    public async Task<SimulationDto> CreateAsync(string leadId, CreateSimulationRequest request)
    {
        var lead = await mongo.GetLeadOrThrowAsync(leadId);

        if (lead.Consultation is null)
        {
            throw new ConsultationNotCompletedException(leadId);
        }

        var now = DateTime.UtcNow;
        var (installmentAmount, totalAmount) = SimulationCalculator.Calculate(request.RequestedAmount, request.Installments);

        var simulation = new Core.Models.Simulation
        {
            Id = Guid.NewGuid().ToString(),
            RequestedAmount = request.RequestedAmount,
            Installments = request.Installments,
            InterestRate = SimulationCalculator.Rate,
            InstallmentAmount = installmentAmount,
            TotalAmount = totalAmount,
            Selected = true,
            SimulatedAt = now,
        };

        // Two sequential atomic updates: MongoDB rejects a single update that combines a $set
        // on "simulations.$[].selected" with a $push to "simulations" (conflicting paths on
        // the same field). A crash between them would at worst leave every simulation
        // unselected momentarily — never a lost or corrupted record.
        await mongo.Leads.UpdateOneAsync(l => l.Id == leadId, Builders<Lead>.Update.Set("simulations.$[].selected", false));

        var update = Builders<Lead>.Update
            .Push(l => l.Simulations, simulation)
            .Set(l => l.Progress.CurrentStep, "simulation")
            .AddToSet(l => l.Progress.StartedSteps, "simulation")
            .AddToSet(l => l.Progress.CompletedSteps, "simulation")
            .Set(l => l.Progress.LastUpdatedAt, now)
            .Set(l => l.UpdatedAt, now)
            .Inc(l => l.Version, 1);

        await mongo.Leads.UpdateOneAsync(l => l.Id == leadId, update);

        return LeadMapper.MapSimulation(simulation);
    }

    public async Task<SimulationDto> SelectAsync(string leadId, string simulationId)
    {
        var lead = await mongo.GetLeadOrThrowAsync(leadId);

        var target = lead.Simulations.FirstOrDefault(s => s.Id == simulationId)
            ?? throw new SimulationNotFoundException(simulationId);

        var now = DateTime.UtcNow;

        // Same two-atomic-update pattern as CreateAsync — a single update can't combine a
        // blanket $set on "simulations.$[].selected" with a targeted array-filter $set on
        // the same field. Only the matching simulation's `selected` flips to true; nothing
        // else in the array is read or rewritten (no read-modify-write of the full array).
        await mongo.Leads.UpdateOneAsync(l => l.Id == leadId, Builders<Lead>.Update.Set("simulations.$[].selected", false));

        // NOTE: the array element's `Id` property maps to BSON `_id` (the driver's default
        // "Id"-named-property convention applies even on nested, non-root classes — it isn't
        // overridden by the camelCase element-name convention pack) — so the array filter must
        // match on "elem._id", not "elem.id".
        var arrayFilters = new List<ArrayFilterDefinition> { new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("elem._id", simulationId)) };
        var update = Builders<Lead>.Update
            .Set("simulations.$[elem].selected", true)
            .Set(l => l.UpdatedAt, now)
            .Inc(l => l.Version, 1);
        await mongo.Leads.UpdateOneAsync(l => l.Id == leadId, update, new UpdateOptions { ArrayFilters = arrayFilters });

        target.Selected = true;
        return LeadMapper.MapSimulation(target);
    }
}
