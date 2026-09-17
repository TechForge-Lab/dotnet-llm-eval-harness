using EvalHarness.Core;
using Xunit;

namespace EvalHarness.Tests;

public class EvalRunnerTests
{
    [Fact]
    public async Task RunAsync_MarksCasePassed_WhenAllAssertionsPass()
    {
        var client = new FakeModelClient(new() { ["2+2?"] = "4" });
        var runner = new EvalRunner(client);
        var cases = new List<EvalCase>
        {
            new()
            {
                Id = "math-1",
                Prompt = "2+2?",
                Assertions = new() { new Assertion { Kind = AssertionKind.Contains, Value = "4" } },
            },
        };

        var result = await runner.RunAsync(cases);

        Assert.Equal(1, result.Passed);
        Assert.Equal(0, result.Failed);
        Assert.Equal(1.0, result.PassRate);
    }

    [Fact]
    public async Task RunAsync_MarksCaseFailed_WhenAnyAssertionFails()
    {
        var client = new FakeModelClient(new() { ["2+2?"] = "5" }); // wrong on purpose
        var runner = new EvalRunner(client);
        var cases = new List<EvalCase>
        {
            new()
            {
                Id = "math-1",
                Prompt = "2+2?",
                Assertions = new() { new Assertion { Kind = AssertionKind.Contains, Value = "4" } },
            },
        };

        var result = await runner.RunAsync(cases);

        Assert.Equal(0, result.Passed);
        Assert.Equal(1, result.Failed);
        Assert.False(result.Cases.Single().Passed);
    }

    [Fact]
    public async Task RunAsync_StopsAtFirstFailingAssertion_ForThatCase()
    {
        var client = new FakeModelClient(new() { ["greet"] = "hi" });
        var runner = new EvalRunner(client);
        var cases = new List<EvalCase>
        {
            new()
            {
                Id = "greet-1",
                Prompt = "greet",
                Assertions = new()
                {
                    new Assertion { Kind = AssertionKind.Contains, Value = "nonexistent" }, // fails first
                    new Assertion { Kind = AssertionKind.Contains, Value = "hi" },          // never evaluated
                },
            },
        };

        var result = await runner.RunAsync(cases);
        var caseResult = result.Cases.Single();

        Assert.False(caseResult.Passed);
        Assert.Single(caseResult.AssertionResults); // proves the second assertion was never run
    }

    [Fact]
    public async Task RunAsync_ComputesPassRate_AcrossMultipleCases()
    {
        var client = new FakeModelClient(new()
        {
            ["a"] = "yes",
            ["b"] = "no",
        });
        var runner = new EvalRunner(client);
        var cases = new List<EvalCase>
        {
            new() { Id = "a", Prompt = "a", Assertions = new() { new Assertion { Kind = AssertionKind.Contains, Value = "yes" } } },
            new() { Id = "b", Prompt = "b", Assertions = new() { new Assertion { Kind = AssertionKind.Contains, Value = "yes" } } },
        };

        var result = await runner.RunAsync(cases);

        Assert.Equal(2, result.Total);
        Assert.Equal(0.5, result.PassRate);
    }
}
