using System.Text.Json;
using System.Text.Json.Nodes;

namespace ConsignadoLeads.Api.Core.Logging;

/// <summary>
/// Redacts CPF/banking fields from any serializable object before it's written to a log.
/// Matches field names case-insensitively so it works regardless of the object's JSON
/// naming policy. Walks nested objects/arrays, so a sensitive field is redacted no matter
/// how deep it's nested (e.g. <c>bankingData.account</c>).
/// </summary>
public static class SensitiveDataMasker
{
    private const string RedactedValue = "***REDACTED***";

    private static readonly string[] SensitiveFieldNames = ["cpf", "documentNumber", "bankingData"];

    /// <summary>Returns a JSON string representation of <paramref name="value"/> with sensitive fields redacted.</summary>
    public static string Mask(object? value)
    {
        var node = JsonSerializer.SerializeToNode(value);
        MaskNode(node);
        return node?.ToJsonString() ?? "null";
    }

    private static void MaskNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var key in jsonObject.Select(property => property.Key).ToList())
                {
                    if (IsSensitiveField(key))
                    {
                        jsonObject[key] = RedactedValue;
                    }
                    else
                    {
                        MaskNode(jsonObject[key]);
                    }
                }

                break;

            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    MaskNode(item);
                }

                break;
        }
    }

    private static bool IsSensitiveField(string fieldName) =>
        SensitiveFieldNames.Any(sensitive => string.Equals(sensitive, fieldName, StringComparison.OrdinalIgnoreCase));
}
