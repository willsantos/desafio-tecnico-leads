namespace ConsignadoLeads.Api.Features.Simulation;

public static class SimulationEndpoints
{
    public static IEndpointRouteBuilder MapSimulationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/leads/{id}/steps/simulation", async (string id, CreateSimulationRequest request, SimulationHandler handler) =>
        {
            var dto = await handler.CreateAsync(id, request);
            return Results.Created($"/leads/{id}/steps/simulation/{dto.Id}", dto);
        }).Produces<SimulationResponseDto>(StatusCodes.Status201Created);

        app.MapPatch("/leads/{id}/steps/simulation/{simulationId}/select", async (string id, string simulationId, SimulationHandler handler) =>
        {
            var dto = await handler.SelectAsync(id, simulationId);
            return Results.Ok(dto);
        }).Produces<SimulationResponseDto>(StatusCodes.Status200OK);

        return app;
    }
}
