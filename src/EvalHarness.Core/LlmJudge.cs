namespace EvalHarness.Core;

/// <summary>
/// Uses a second model call to grade responses that can't be checked with a
/// string match — tone, faithfulness to source material, whether an answer
/// is "helpful." This is the most expensive and least deterministic check
/// in the harness, which is exactly why AssertionScorer always runs the
/// cheap checks first: most regressions get caught before this ever runs.
/// </summary>
public static class LlmJudge
{
    private const string JudgeSystemPrompt =
        """
        You are a strict grading assistant. You will be given a grading instruction
        and a candidate response. Decide if the candidate response satisfies the
        instruction. Reply with exactly one line in the form:
        PASS: <one sentence reason>
        or
        FAIL: <one sentence reason>
        Do not add anything else.
        """;

    public static async Task<AssertionResult> ScoreAsync(
        IModelClient? judgeClient, string judgePrompt, string candidateResponse, CancellationToken ct)
    {
        if (judgeClient is null)
            return new AssertionResult(false, "No judge model configured — set a judge client to use LlmJudge assertions.");

        var prompt = $"Grading instruction: {judgePrompt}\n\nCandidate response:\n{candidateResponse}";
        var verdict = await judgeClient.CompleteAsync(JudgeSystemPrompt, prompt, ct);

        var trimmed = verdict.Trim();
        if (trimmed.StartsWith("PASS", StringComparison.OrdinalIgnoreCase))
            return new AssertionResult(true, trimmed);
        if (trimmed.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase))
            return new AssertionResult(false, trimmed);

        return new AssertionResult(false, $"Judge returned an unparseable verdict: \"{trimmed}\"");
    }
}
