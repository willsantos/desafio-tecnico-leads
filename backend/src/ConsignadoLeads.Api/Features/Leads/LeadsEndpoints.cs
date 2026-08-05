namespace ConsignadoLeads.Api.Features.Leads;

public static class LeadsEndpoints
{
    public static IEndpointRouteBuilder MapLeadsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/leads", async (string? status, string? currentStep, string? cpf, int? page, int? pageSize, LeadsHandler handler) =>
        {
            var result = await handler.ListAsync(new LeadsQuery(status, currentStep, cpf, page, pageSize));
            return Results.Ok(result);
        });

        app.MapGet("/leads/{id}", async (string id, LeadsHandler handler) =>
        {
            var dto = await handler.GetByIdAsync(id);
            return Results.Ok(dto);
        });

        return app;
    }
}
