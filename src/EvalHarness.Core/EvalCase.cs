namespace EvalHarness.Core;

/// <summary>
/// One "golden" test case: a prompt you send to the model, and the rule(s)
/// used to decide whether the response is acceptable.
///
/// This is intentionally plain data (no behavior) so that non-engineers on a
/// team — QA, product, domain experts — can author cases as YAML without
/// touching C#.
/// </summary>
public sealed class EvalCase
{
    /// <summary>Unique, human-readable id, e.g. "invoice-missing-total".</summary>
    public required string Id { get; init; }

    /// <summary>Short description shown in reports.</summary>
    public string? Description { get; init; }

    /// <summary>The prompt sent to the model under test.</summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Optional system prompt override for this case. Falls back to the
    /// suite-level default when null.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// The checks that must all pass for this case to be scored "pass".
    /// A case can combine cheap deterministic checks with an LLM-as-judge
    /// check — deterministic checks always run first because they're free
    /// and catch most regressions.
    /// </summary>
    public required List<Assertion> Assertions { get; init; }

    /// <summary>Free-form labels for filtering, e.g. ["regression", "pii"].</summary>
    public List<string> Tags { get; init; } = new();
}

/// <summary>
/// A single pass/fail check against a model response.
/// Exactly one of the typed properties below should be set; Kind tells the
/// runner which one to use. This mirrors how the YAML is authored.
/// </summary>
public sealed class Assertion
{
    public required AssertionKind Kind { get; init; }

    /// <summary>Used by Contains / NotContains / ExactMatch / Regex.</summary>
    public string? Value { get; init; }

    /// <summary>Used by JsonSchemaValid — path to a JSON Schema file.</summary>
    public string? SchemaPath { get; init; }

    /// <summary>Used by LlmJudge — the grading instruction given to the judge model.</summary>
    public string? JudgePrompt { get; init; }

    /// <summary>Case-insensitive comparison for text-based assertions. Defaults to true.</summary>
    public bool IgnoreCase { get; init; } = true;
}

public enum AssertionKind
{
    Contains,
    NotContains,
    ExactMatch,
    Regex,
    JsonSchemaValid,
    LlmJudge,
}
