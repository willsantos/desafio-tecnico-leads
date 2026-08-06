using ConsignadoLeads.Api.Core.Dtos;

namespace ConsignadoLeads.Api.Features.ProfessionalBankingData;

public static class ProfessionalBankingDataEndpoints
{
    public static IEndpointRouteBuilder MapProfessionalBankingDataEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/leads/{id}/steps/professional-banking-data", async (string id, ProfessionalBankingDataRequest request, ProfessionalBankingDataHandler handler) =>
        {
            var dto = await handler.UpdateAsync(id, request);
            return Results.Ok(dto);
        }).Produces<LeadDto>(StatusCodes.Status200OK);

        return app;
    }
}
