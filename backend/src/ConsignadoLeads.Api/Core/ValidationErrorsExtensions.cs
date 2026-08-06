using Microsoft.AspNetCore.Mvc;

namespace ConsignadoLeads.Api.Core;

/// <summary>
/// Shared 400 shape for endpoint-level input validation (request field checks run before any
/// Mongo/GridFS call) — used by every slice that validates its own request instead of each
/// one building the same <see cref="Microsoft.AspNetCore.Http.Results.ValidationProblem"/>
/// dictionary inline.
/// </summary>
public static class ValidationErrorsExtensions
{
    private const string TypeBaseUri = "https://errors.consignado-leads/";

    public static IResult ToValidationProblem(this IReadOnlyList<string> errors)
    {
        var problemDetails = new ProblemDetails
        {
            Type = TypeBaseUri + "request-validation",
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Um ou mais campos da requisição são inválidos.",
        };

        // Mirrors the nesting style used by ProblemDetailsExceptionHandler so the JSON shape
        // stays consistent with the README seção 5 Problem Details example.
        problemDetails.Extensions["extensions"] = new Dictionary<string, object?>
        {
            ["errors"] = new Dictionary<string, string[]> { ["request"] = [.. errors] }
        };

        return TypedResults.Problem(problemDetails);
    }
}
