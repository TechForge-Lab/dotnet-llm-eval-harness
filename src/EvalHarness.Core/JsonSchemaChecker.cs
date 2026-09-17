using System.Text.Json;

namespace EvalHarness.Core;

/// <summary>
/// Deliberately minimal JSON structural check: confirms the response parses
/// as JSON and that every property listed as "required" in the schema file
/// is present. This is not a full JSON Schema implementation — swap in
/// NJsonSchema or JsonSchema.Net if you need full draft-07 support. For
/// grading "did the model return a well-formed extraction," this covers the
/// case that actually breaks production: missing or malformed fields.
/// </summary>
public static class JsonSchemaChecker
{
    public static AssertionResult Check(string response, string schemaPath)
    {
        JsonDocument responseDoc;
        try
        {
            responseDoc = JsonDocument.Parse(response);
        }
        catch (JsonException ex)
        {
            return new AssertionResult(false, $"Response is not valid JSON: {ex.Message}");
        }

        if (!File.Exists(schemaPath))
            return new AssertionResult(false, $"Schema file not found: {schemaPath}");

        using var schemaDoc = JsonDocument.Parse(File.ReadAllText(schemaPath));
        if (!schemaDoc.RootElement.TryGetProperty("required", out var requiredArray))
            return new AssertionResult(true, "Schema has no required fields; JSON parsed successfully.");

        var missing = new List<string>();
        foreach (var field in requiredArray.EnumerateArray())
        {
            var name = field.GetString() ?? "";
            if (!responseDoc.RootElement.TryGetProperty(name, out _))
                missing.Add(name);
        }

        return missing.Count == 0
            ? new AssertionResult(true, "All required fields present.")
            : new AssertionResult(false, $"Missing required field(s): {string.Join(", ", missing)}.");
    }
}
