namespace EvalHarness.Core;

/// <summary>
/// Anything that can turn a prompt into a text completion. Implement this once
/// per provider (Azure OpenAI, Anthropic, a local model) and the rest of the
/// harness doesn't care which one you're using.
/// </summary>
public interface IModelClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}

/// <summary>
/// A deterministic fake client for tests and for demoing the harness without
/// API keys. Maps prompts to canned responses; anything unmapped returns a
/// fixed fallback so the suite still runs end to end.
/// </summary>
public sealed class FakeModelClient : IModelClient
{
    private readonly Dictionary<string, string> _responses;
    private readonly string _fallback;

    public FakeModelClient(Dictionary<string, string> responses, string fallback = "I don't know.")
    {
        _responses = responses;
        _fallback = fallback;
    }

    public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var response = _responses.TryGetValue(userPrompt, out var value) ? value : _fallback;
        return Task.FromResult(response);
    }
}
