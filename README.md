# .NET LLM Eval Harness

A small, CI-friendly framework for grading LLM outputs from a .NET application
— the same way you'd grade with `promptfoo` or `ragas` in a Python stack, but
native to C# and runnable as a normal build step.

## Why this exists

Most .NET teams shipping LLM features test them by hand: someone tries a
prompt, eyeballs the output, ships it. The next prompt tweak silently breaks
a case nobody re-checked. This project treats prompts and model responses
like any other code change — with golden test cases, automated grading, and
a build that fails when quality regresses.

## What it does

- Define test cases as plain YAML — no C# required to add a case
- Grade responses with four kinds of checks, cheapest first:
  - `Contains` / `NotContains` / `ExactMatch` / `Regex` — free, instant, deterministic
  - `JsonSchemaValid` — confirms structured extraction returned the required fields
  - `LlmJudge` — a second model call grades subjective qualities (tone, faithfulness) when a string match can't
- Run the whole suite from the command line or as a GitHub Actions step
- Get a non-zero exit code on any failure, so it gates a pull request like a unit test suite does

## Quickstart

```bash
git clone https://github.com/<your-username>/dotnet-llm-eval-harness.git
cd dotnet-llm-eval-harness
dotnet run --project src/EvalHarness.Cli
```

This runs the example suite in `examples/cases` against a built-in fake model
client — no API key needed. You should see output like:

```
Loading eval cases from: /.../examples/cases
Loaded 4 case(s).

[PASS] invoice-extraction-happy-path  (2 ms)
[FAIL] invoice-extraction-missing-total  (1 ms)
       reason: Missing required field(s): total.
       response was: {"invoiceNumber": null}
[PASS] summarize-lab-result  (1 ms)
[PASS] summarize-stays-on-topic  (1 ms)

3/4 passed  (75%)
```

That one failure is intentional — it demonstrates the harness catching an
extraction that silently dropped a required field, which is the exact failure
mode a confidence-gated pipeline in production needs to route to a human
reviewer rather than let through.

## Writing your own cases

Cases live in `examples/cases/*.yaml`. A minimal case:

```yaml
cases:
  - id: refund-policy-answer
    description: The bot must never promise a refund timeline we can't guarantee.
    prompt: "How long does a refund take?"
    tags: [policy, regression]
    assertions:
      - kind: NotContains
        value: "same day"
      - kind: Contains
        value: "business days"
```

Run just your new file by pointing the CLI at its folder:

```bash
dotnet run --project src/EvalHarness.Cli -- path/to/your/cases
```

## Pointing it at a real model

`Program.cs` wires up a `FakeModelClient` so the repo runs with zero setup.
To grade a real model, implement `IModelClient` for your provider:

```csharp
public sealed class AzureOpenAiClient : IModelClient
{
    private readonly ChatClient _client; // Azure.AI.OpenAI

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var response = await _client.CompleteChatAsync(
            new[] { new SystemChatMessage(systemPrompt), new UserChatMessage(userPrompt) },
            cancellationToken: ct);
        return response.Value.Content[0].Text;
    }
}
```

Swap the `FakeModelClient` in `Program.cs` for your implementation, and read
the API key from an environment variable — never commit it (see `.gitignore`).

## Running in CI

`.github/workflows/ci.yml` runs `dotnet test` and the example eval suite on
every push and pull request. In a real project this is the step that turns
"the AI feature seems to work" into "the AI feature is verified to work,"
the same way a unit test suite does for ordinary code.

## Design decisions

**Why YAML instead of C# for cases?** So a QA engineer, product manager, or
domain expert can add or edit a golden case without opening the IDE. The
tradeoff is weaker compile-time safety on case files — `EvalSuiteLoader`
validates structure at load time to catch the most common authoring mistakes
early instead.

**Why does a case stop at its first failing assertion?** A report that says
"the first thing that broke was X" is far more useful in a CI log than five
assertions failing for the same underlying reason. If you need every
assertion graded regardless of earlier failures (e.g. for a scoring
dashboard), that's a straightforward change to `EvalRunner.RunAsync`.

**Why is `JsonSchemaValid` a hand-rolled checker instead of a full JSON
Schema library?** It covers the failure mode that actually matters for
LLM output — missing or malformed required fields — without a dependency.
Swap in `JsonSchema.Net` if you need full draft-07 support (nested schemas,
`oneOf`, format validators, etc.).

**Why is `LlmJudge` a separate, more expensive path?** Judging with a second
LLM call is non-deterministic and costs money on every run. Keeping
deterministic checks first means most regressions get caught for free, and
the judge model only runs on cases that specifically need subjective
grading (tone, faithfulness to source material, helpfulness).

## What's not here yet

- No retry/backoff around real provider calls — add this in your `IModelClient` implementation
- No historical pass-rate tracking across runs — the next step would be writing `SuiteResult` to a file per run and diffing
- No parallel case execution — cases currently run sequentially, which is fine for a few dozen cases but worth parallelizing past that

## Project layout

```
src/
  EvalHarness.Core/    the library: models, YAML loader, scorer, runner
  EvalHarness.Cli/      console entry point used locally and in CI
tests/
  EvalHarness.Tests/    xUnit tests for the scorer and runner
examples/
  cases/                sample YAML eval suites
  schemas/              JSON schemas referenced by JsonSchemaValid cases
.github/workflows/      CI pipeline
```

## License

MIT — see `LICENSE`.
