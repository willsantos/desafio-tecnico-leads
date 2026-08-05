using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ConsignadoLeads.Api.Core.Exceptions;

/// <summary>
/// Maps each domain exception type to its documented HTTP status code and RFC 9457
/// Problem Details shape. Exception types not recognized here fall through to ASP.NET
/// Core's default problem-details fallback (generic 500).
/// </summary>
public class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    private const string TypeBaseUri = "https://errors.consignado-leads/";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = Map(exception);
        if (problemDetails is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }

    private static ProblemDetails? Map(Exception exception) => exception switch
    {
        LeadNotFoundException e => Build(StatusCodes.Status404NotFound, "Not Found", "lead-not-found", e.Message),
        DocumentNotFoundException e => Build(StatusCodes.Status404NotFound, "Not Found", "document-not-found", e.Message),
        VersionConflictException e => Build(StatusCodes.Status409Conflict, "Conflict", "version-conflict", e.Message,
            extensions: new Dictionary<string, object?> { ["currentVersion"] = e.CurrentVersion }),
        ConfirmationInProgressException e => Build(StatusCodes.Status409Conflict, "Conflict", "confirmation-in-progress", e.Message),
        LeadAlreadyCompletedException e => Build(StatusCodes.Status409Conflict, "Conflict", "lead-already-completed", e.Message),
        PendingRequirementsException e => Build(StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity", "pending-requirements", e.Message,
            extensions: new Dictionary<string, object?> { ["reasons"] = e.Reasons }),
        MainSystemUnavailableException e => Build(StatusCodes.Status503ServiceUnavailable, "Service Unavailable", "main-system-unavailable", e.Message),
        MainSystemTimeoutException e => Build(StatusCodes.Status504GatewayTimeout, "Gateway Timeout", "main-system-timeout", e.Message),
        _ => null,
    };

    private static ProblemDetails Build(
        int status,
        string title,
        string typeSlug,
        string detail,
        IDictionary<string, object?>? extensions = null)
    {
        var problemDetails = new ProblemDetails
        {
            Type = TypeBaseUri + typeSlug,
            Title = title,
            Status = status,
            Detail = detail,
        };

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        return problemDetails;
    }
}
