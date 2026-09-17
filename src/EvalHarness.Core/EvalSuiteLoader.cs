using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EvalHarness.Core;

/// <summary>
/// Loads a directory of *.yaml files into a flat list of EvalCases.
/// Each file may contain one case or a list of cases under a "cases:" key —
/// see examples/cases for the exact shape.
/// </summary>
public static class EvalSuiteLoader
{
    public static List<EvalCase> LoadFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Case directory not found: {directoryPath}");

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var cases = new List<EvalCase>();

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.yaml", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(directoryPath, "*.yml", SearchOption.AllDirectories))
                     .OrderBy(f => f))
        {
            var yaml = File.ReadAllText(file);
            var doc = deserializer.Deserialize<CaseFile>(yaml);

            if (doc?.Cases is null || doc.Cases.Count == 0)
            {
                Console.Error.WriteLine($"[warn] {Path.GetFileName(file)} contained no cases — skipped.");
                continue;
            }

            foreach (var raw in doc.Cases)
                cases.Add(raw.ToEvalCase());
        }

        var duplicateIds = cases.GroupBy(c => c.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateIds.Count > 0)
            throw new InvalidOperationException($"Duplicate case ids found: {string.Join(", ", duplicateIds)}");

        return cases;
    }

    // --- YAML-shaped intermediate types (kept separate from EvalCase so the
    //     public API stays clean and typed, while YAML stays loose/stringly-typed) ---

    private sealed class CaseFile
    {
        public List<RawCase>? Cases { get; set; }
    }

    private sealed class RawCase
    {
        public string Id { get; set; } = "";
        public string? Description { get; set; }
        public string Prompt { get; set; } = "";
        public string? SystemPrompt { get; set; }
        public List<string>? Tags { get; set; }
        public List<RawAssertion>? Assertions { get; set; }

        public EvalCase ToEvalCase() => new()
        {
            Id = Id,
            Description = Description,
            Prompt = Prompt,
            SystemPrompt = SystemPrompt,
            Tags = Tags ?? new(),
            Assertions = (Assertions ?? new()).Select(a => a.ToAssertion()).ToList(),
        };
    }

    private sealed class RawAssertion
    {
        public string Kind { get; set; } = "";
        public string? Value { get; set; }
        public string? SchemaPath { get; set; }
        public string? JudgePrompt { get; set; }
        public bool IgnoreCase { get; set; } = true;

        public Assertion ToAssertion() => new()
        {
            Kind = Enum.Parse<AssertionKind>(Kind, ignoreCase: true),
            Value = Value,
            SchemaPath = SchemaPath,
            JudgePrompt = JudgePrompt,
            IgnoreCase = IgnoreCase,
        };
    }
}
