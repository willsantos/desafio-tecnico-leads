using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Exceptions;
using ConsignadoLeads.Api.Core.Models;
using MongoDB.Driver;

namespace ConsignadoLeads.Api.Features.Simulation;

public class SimulationHandler(MongoContext mongo)
{
    public async Task<SimulationResponseDto> CreateAsync(string leadId, CreateSimulationRequest request)
    {
        var lead = await mongo.Leads.Find(l => l.Id == leadId).FirstOrDefaultAsync()
            ?? throw new LeadNotFoundException(leadId);

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

        return SimulationResponseMapper.ToDto(simulation);
    }

    public async Task<SimulationResponseDto> SelectAsync(string leadId, string simulationId)
    {
        var lead = await mongo.Leads.Find(l => l.Id == leadId).FirstOrDefaultAsync()
            ?? throw new LeadNotFoundException(leadId);

        var target = lead.Simulations.FirstOrDefault(s => s.Id == simulationId)
            ?? throw new SimulationNotFoundException(simulationId);

        foreach (var simulation in lead.Simulations)
        {
            simulation.Selected = simulation.Id == simulationId;
        }

        var now = DateTime.UtcNow;
        var update = Builders<Lead>.Update
            .Set(l => l.Simulations, lead.Simulations)
            .Set(l => l.UpdatedAt, now)
            .Inc(l => l.Version, 1);

        await mongo.Leads.UpdateOneAsync(l => l.Id == leadId, update);

        target.Selected = true;
        return SimulationResponseMapper.ToDto(target);
    }
}
