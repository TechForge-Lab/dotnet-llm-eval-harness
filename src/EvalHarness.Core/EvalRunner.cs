using System.Diagnostics;

namespace EvalHarness.Core;

public sealed record CaseResult(
    string CaseId,
    bool Passed,
    string Response,
    List<(Assertion Assertion, AssertionResult Result)> AssertionResults,
    TimeSpan Duration);

public sealed record SuiteResult(List<CaseResult> Cases)
{
    public int Total => Cases.Count;
    public int Passed => Cases.Count(c => c.Passed);
    public int Failed => Total - Passed;
    public double PassRate => Total == 0 ? 0 : (double)Passed / Total;
}

/// <summary>
/// Runs every case in a suite against the model under test, applies each
/// case's assertions in order, and stops at the first failing assertion per
/// case (so a report tells you the *first* thing that broke, not a wall of
/// noise). A case passes only if every assertion passes.
/// </summary>
public sealed class EvalRunner
{
    private readonly IModelClient _modelUnderTest;
    private readonly AssertionScorer _scorer;
    private readonly string _defaultSystemPrompt;

    public EvalRunner(IModelClient modelUnderTest, IModelClient? judgeClient = null, string defaultSystemPrompt = "")
    {
        _modelUnderTest = modelUnderTest;
        _scorer = new AssertionScorer(judgeClient);
        _defaultSystemPrompt = defaultSystemPrompt;
    }

    public async Task<SuiteResult> RunAsync(IEnumerable<EvalCase> cases, CancellationToken ct = default)
    {
        var results = new List<CaseResult>();

        foreach (var evalCase in cases)
        {
            var sw = Stopwatch.StartNew();
            var systemPrompt = evalCase.SystemPrompt ?? _defaultSystemPrompt;
            var response = await _modelUnderTest.CompleteAsync(systemPrompt, evalCase.Prompt, ct);

            var assertionResults = new List<(Assertion, AssertionResult)>();
            var casePassed = true;

            foreach (var assertion in evalCase.Assertions)
            {
                var result = await _scorer.ScoreAsync(assertion, response, ct);
                assertionResults.Add((assertion, result));
                if (!result.Passed)
                {
                    casePassed = false;
                    break; // first failure explains the case; no need to keep grading
                }
            }

            sw.Stop();
            results.Add(new CaseResult(evalCase.Id, casePassed, response, assertionResults, sw.Elapsed));
        }

        return new SuiteResult(results);
    }
}
