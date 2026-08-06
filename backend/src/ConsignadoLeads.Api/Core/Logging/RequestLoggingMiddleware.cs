using System.Diagnostics;
using System.Text.Json;

namespace ConsignadoLeads.Api.Core.Logging;

/// <summary>
/// Logs method, path, status code and duration for every request at Information level.
/// At Debug level only (opt-in, off by default), also logs the JSON request body through
/// <see cref="SensitiveDataMasker"/> — never the raw body — for local troubleshooting
/// without ever writing CPF/banking data in plain text (spec P1-9 AC3). Multipart requests
/// (document uploads) are skipped entirely: the body is a file stream, not a loggable JSON
/// payload, and touching it here would risk interfering with the upload handler downstream.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var maskedBody = await TryReadMaskedJsonBodyAsync(context);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "{Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            if (maskedBody is not null)
            {
                logger.LogDebug("{Method} {Path} request body: {MaskedBody}", context.Request.Method, context.Request.Path, maskedBody);
            }
        }
    }

    private async Task<string?> TryReadMaskedJsonBodyAsync(HttpContext context)
    {
        if (!logger.IsEnabled(LogLevel.Debug))
        {
            return null;
        }

        if (context.Request.ContentType is not { } contentType || !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        try
        {
            return SensitiveDataMasker.Mask(JsonSerializer.Deserialize<JsonElement>(rawBody));
        }
        catch (JsonException)
        {
            // Not valid JSON despite the content-type header — skip rather than fail the request.
            return null;
        }
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();
}
