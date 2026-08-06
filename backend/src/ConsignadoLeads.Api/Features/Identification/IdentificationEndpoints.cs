using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;

namespace ConsignadoLeads.Api.Features.Identification;

public static class IdentificationEndpoints
{
    public static IEndpointRouteBuilder MapIdentificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/leads/{id}/steps/identification", async (string id, IdentificationRequest request, IdentificationHandler handler) =>
        {
            var errors = IdentificationValidator.Validate(request);
            if (errors.Count > 0)
            {
                return errors.ToValidationProblem();
            }

            var dto = await handler.UpdateAsync(id, request);
            return Results.Ok(dto);
        }).Produces<LeadDto>(StatusCodes.Status200OK);

        return app;
    }
}
