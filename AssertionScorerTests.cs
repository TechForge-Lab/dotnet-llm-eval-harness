using EvalHarness.Core;
using Xunit;

namespace EvalHarness.Tests;

public class AssertionScorerTests
{
    private readonly AssertionScorer _scorer = new();

    [Fact]
    public async Task Contains_Passes_WhenSubstringPresent()
    {
        var assertion = new Assertion { Kind = AssertionKind.Contains, Value = "hello" };
        var result = await _scorer.ScoreAsync(assertion, "well, hello there");
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task Contains_Fails_WhenSubstringAbsent()
    {
        var assertion = new Assertion { Kind = AssertionKind.Contains, Value = "goodbye" };
        var result = await _scorer.ScoreAsync(assertion, "well, hello there");
        Assert.False(result.Passed);
    }

    [Fact]
    public async Task Contains_IsCaseInsensitiveByDefault()
    {
        var assertion = new Assertion { Kind = AssertionKind.Contains, Value = "HELLO" };
        var result = await _scorer.ScoreAsync(assertion, "well, hello there");
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task Contains_RespectsCaseSensitivity_WhenIgnoreCaseFalse()
    {
        var assertion = new Assertion { Kind = AssertionKind.Contains, Value = "HELLO", IgnoreCase = false };
        var result = await _scorer.ScoreAsync(assertion, "well, hello there");
        Assert.False(result.Passed);
    }

    [Fact]
    public async Task NotContains_Fails_WhenForbiddenTextPresent()
    {
        var assertion = new Assertion { Kind = AssertionKind.NotContains, Value = "normal" };
        var result = await _scorer.ScoreAsync(assertion, "The result was normal.");
        Assert.False(result.Passed);
    }

    [Fact]
    public async Task Regex_Passes_WhenPatternMatches()
    {
        var assertion = new Assertion { Kind = AssertionKind.Regex, Value = @"\$\d+\.\d{2}" };
        var result = await _scorer.ScoreAsync(assertion, "Total: $1204.50");
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task ExactMatch_IgnoresSurroundingWhitespace()
    {
        var assertion = new Assertion { Kind = AssertionKind.ExactMatch, Value = "Paris" };
        var result = await _scorer.ScoreAsync(assertion, "  Paris  \n");
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task JsonSchemaValid_Fails_OnMalformedJson()
    {
        var assertion = new Assertion { Kind = AssertionKind.JsonSchemaValid, SchemaPath = "does-not-matter.json" };
        var result = await _scorer.ScoreAsync(assertion, "{ not valid json");
        Assert.False(result.Passed);
        Assert.Contains("not valid JSON", result.Reason);
    }

    [Fact]
    public async Task LlmJudge_FailsGracefully_WhenNoJudgeClientConfigured()
    {
        var assertion = new Assertion { Kind = AssertionKind.LlmJudge, JudgePrompt = "Is this polite?" };
        var result = await _scorer.ScoreAsync(assertion, "Sure, happy to help.");
        Assert.False(result.Passed);
        Assert.Contains("No judge model configured", result.Reason);
    }
}
