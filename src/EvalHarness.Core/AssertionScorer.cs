using System.Text.RegularExpressions;

namespace EvalHarness.Core;

public sealed record AssertionResult(bool Passed, string Reason);

/// <summary>
/// Runs a single Assertion against a model's response and explains why it
/// passed or failed. Deterministic checks (Contains, Regex, JsonSchemaValid,
/// etc.) never call the network — only LlmJudge does, via the supplied judge
/// client. Keeping the cheap checks free is what makes it realistic to run
/// this on every pull request.
/// </summary>
public sealed class AssertionScorer
{
    private readonly IModelClient? _judgeClient;

    public AssertionScorer(IModelClient? judgeClient = null)
    {
        _judgeClient = judgeClient;
    }

    public async Task<AssertionResult> ScoreAsync(Assertion assertion, string response, CancellationToken ct = default)
    {
        var comparison = assertion.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        switch (assertion.Kind)
        {
            case AssertionKind.Contains:
                var contains = response.Contains(assertion.Value ?? "", comparison);
                return new(contains, contains
                    ? $"Response contains \"{assertion.Value}\"."
                    : $"Expected response to contain \"{assertion.Value}\" but it did not.");

            case AssertionKind.NotContains:
                var notContains = !response.Contains(assertion.Value ?? "", comparison);
                return new(notContains, notContains
                    ? $"Response correctly omits \"{assertion.Value}\"."
                    : $"Response contains forbidden text \"{assertion.Value}\".");

            case AssertionKind.ExactMatch:
                var exact = string.Equals(response.Trim(), assertion.Value?.Trim(), comparison);
                return new(exact, exact
                    ? "Response matches expected text exactly."
                    : $"Expected exact match \"{assertion.Value}\" but got \"{Truncate(response)}\".");

            case AssertionKind.Regex:
                var pattern = assertion.Value ?? "";
                var options = assertion.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                var isMatch = Regex.IsMatch(response, pattern, options);
                return new(isMatch, isMatch
                    ? $"Response matches pattern /{pattern}/."
                    : $"Response did not match pattern /{pattern}/.");

            case AssertionKind.JsonSchemaValid:
                return JsonSchemaChecker.Check(response, assertion.SchemaPath!);

            case AssertionKind.LlmJudge:
                return await LlmJudge.ScoreAsync(_judgeClient, assertion.JudgePrompt ?? "", response, ct);

            default:
                throw new NotSupportedException($"Unknown assertion kind: {assertion.Kind}");
        }
    }

    private static string Truncate(string s) => s.Length <= 80 ? s : s[..80] + "…";
}
