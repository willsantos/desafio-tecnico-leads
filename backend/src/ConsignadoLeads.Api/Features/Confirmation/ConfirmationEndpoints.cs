using ConsignadoLeads.Api.Core.Dtos;

namespace ConsignadoLeads.Api.Features.Confirmation;

public static class ConfirmationEndpoints
{
    public static IEndpointRouteBuilder MapConfirmationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/leads/{id}/confirm", async (string id, ConfirmRequest? request, ConfirmationHandler handler) =>
        {
            var result = await handler.ConfirmAsync(id, request?.MockOutcome);
            return Results.Json(result.Dto, statusCode: result.StatusCode);
        }).Produces<LeadDto>(StatusCodes.Status200OK).Produces<LeadDto>(StatusCodes.Status202Accepted);

        app.MapPost("/leads/{id}/retry-submission", async (string id, ConfirmationHandler handler) =>
        {
            var result = await handler.RetrySubmissionAsync(id);
            return Results.Json(result.Dto, statusCode: result.StatusCode);
        }).Produces<LeadDto>(StatusCodes.Status200OK);

        return app;
    }
}
