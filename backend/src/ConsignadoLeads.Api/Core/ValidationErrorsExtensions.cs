namespace ConsignadoLeads.Api.Core;

/// <summary>
/// Shared 400 shape for endpoint-level input validation (request field checks run before any
/// Mongo/GridFS call) — used by every slice that validates its own request instead of each
/// one building the same <see cref="Microsoft.AspNetCore.Http.Results.ValidationProblem"/>
/// dictionary inline.
/// </summary>
public static class ValidationErrorsExtensions
{
    public static IResult ToValidationProblem(this IReadOnlyList<string> errors) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [.. errors] });
}
