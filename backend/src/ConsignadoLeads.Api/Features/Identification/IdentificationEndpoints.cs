namespace ConsignadoLeads.Api.Features.Identification;

public static class IdentificationEndpoints
{
    public static IEndpointRouteBuilder MapIdentificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/leads/{id}/steps/identification", async (string id, IdentificationRequest request, IdentificationHandler handler) =>
        {
            var dto = await handler.UpdateAsync(id, request);
            return Results.Ok(dto);
        });

        return app;
    }
}
