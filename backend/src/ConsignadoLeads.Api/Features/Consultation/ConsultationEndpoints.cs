namespace ConsignadoLeads.Api.Features.Consultation;

public static class ConsultationEndpoints
{
    public static IEndpointRouteBuilder MapConsultationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/leads/consultation", async (ConsultationRequest request, ConsultationHandler handler) =>
        {
            var errors = ConsultationValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [.. errors] });
            }

            var dto = await handler.CreateAsync(request);
            return Results.Created($"/leads/{dto.Id}", dto);
        });

        app.MapPut("/leads/{id}/steps/consultation", async (string id, ConsultationRequest request, ConsultationHandler handler) =>
        {
            var errors = ConsultationValidator.Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [.. errors] });
            }

            var dto = await handler.UpdateAsync(id, request);
            return Results.Ok(dto);
        });

        return app;
    }
}
