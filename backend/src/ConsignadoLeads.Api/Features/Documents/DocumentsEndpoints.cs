using ConsignadoLeads.Api.Core;
using ConsignadoLeads.Api.Core.Dtos;

namespace ConsignadoLeads.Api.Features.Documents;

public static class DocumentsEndpoints
{
    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/leads/{id}/documents", async (string id, HttpRequest request, DocumentsHandler handler) =>
        {
            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            var type = form["type"].ToString();
            var rawSubtype = form["personalDocumentSubtype"].ToString();
            var personalDocumentSubtype = string.IsNullOrWhiteSpace(rawSubtype) ? null : rawSubtype;

            var errors = DocumentsValidator.Validate(file, type, personalDocumentSubtype);
            if (errors.Count > 0)
            {
                return errors.ToValidationProblem();
            }

            var dto = await handler.UploadAsync(id, file!, type, personalDocumentSubtype);
            return Results.Created($"/leads/{id}/documents/{dto.Id}", dto);
        }).Produces<DocumentDto>(StatusCodes.Status201Created);

        app.MapGet("/leads/{id}/documents", async (string id, DocumentsHandler handler) =>
        {
            var documents = await handler.ListActiveAsync(id);
            return Results.Ok(documents);
        }).Produces<IReadOnlyList<DocumentDto>>(StatusCodes.Status200OK);

        app.MapDelete("/leads/{id}/documents/{documentId}", async (string id, string documentId, DocumentsHandler handler) =>
        {
            await handler.DeleteAsync(id, documentId);
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent);

        return app;
    }
}
