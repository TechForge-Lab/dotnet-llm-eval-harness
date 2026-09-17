using EvalHarness.Core;

var casesDir = args.Length > 0 ? args[0] : "examples/cases";

Console.WriteLine($"Loading eval cases from: {Path.GetFullPath(casesDir)}");
var cases = EvalSuiteLoader.LoadFromDirectory(casesDir);
Console.WriteLine($"Loaded {cases.Count} case(s).\n");

// --- Model under test ---------------------------------------------------
// This demo uses a FakeModelClient so the harness runs with zero API keys
// and zero cost. Swap this block for AzureOpenAiClient or AnthropicClient
// (see README) to point the harness at a real model.
var modelUnderTest = new FakeModelClient(new Dictionary<string, string>
{
    ["Extract the invoice total from: 'Invoice #4471, Total Due: $1,204.50'"] =
        """{"invoiceNumber": "4471", "total": 1204.50}""",
    ["Extract the invoice total from: 'Thanks for your business!'"] =
        """{"invoiceNumber": null}""", // deliberately missing "total" to demonstrate a failing case
    ["Summarize in one sentence: The lab result was inconclusive and requires a repeat draw."] =
        "The result was inconclusive, so a repeat blood draw is needed.",
    ["What is the capital of France?"] =
        "The capital of France is Paris.",
});

var runner = new EvalRunner(modelUnderTest, judgeClient: null,
    defaultSystemPrompt: "You are a precise assistant. Follow instructions exactly.");

var result = await runner.RunAsync(cases);

// --- Report ---------------------------------------------------------------
foreach (var c in result.Cases)
{
    var status = c.Passed ? "PASS" : "FAIL";
    Console.WriteLine($"[{status}] {c.CaseId}  ({c.Duration.TotalMilliseconds:F0} ms)");
    if (!c.Passed)
    {
        var firstFailure = c.AssertionResults.First(a => !a.Result.Passed);
        Console.WriteLine($"       reason: {firstFailure.Result.Reason}");
        Console.WriteLine($"       response was: {Truncate(c.Response)}");
    }
}

Console.WriteLine();
Console.WriteLine($"{result.Passed}/{result.Total} passed  ({result.PassRate:P0})");

// Non-zero exit code is what makes `dotnet run` usable as a CI gate.
return result.Failed == 0 ? 0 : 1;

static string Truncate(string s) => s.Length <= 100 ? s : s[..100] + "…";
