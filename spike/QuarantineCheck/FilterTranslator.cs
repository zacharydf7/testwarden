namespace QuarantineCheck;

// The per-framework translations, each combined with the user's own filter of the same family.
public static class FilterTranslator
{
    public static List<string> XunitSimple(IReadOnlyList<QuarantineEntry> entries, IReadOnlyList<string> userArguments)
    {
        var arguments = userArguments.ToList();
        foreach (var entry in entries)
        {
            arguments.AddRange(["--filter-not-method", entry.FullName]);
        }

        return arguments;
    }

    // Query segments can't be combined across segments, so "not Class.Method" becomes several OR'ed queries:
    // each quarantined class minus its methods, each affected namespace minus those classes, and every other namespace.
    public static List<string> XunitQuery(IReadOnlyList<QuarantineEntry> entries, string? userTrait)
    {
        var trait = userTrait is null ? "" : $"/[{userTrait}]";
        var queries = new List<string>();
        foreach (var byNamespace in entries.GroupBy(e => e.Namespace))
        {
            foreach (var byClass in byNamespace.GroupBy(e => e.Class))
            {
                queries.Add($"/*/{byNamespace.Key}/{byClass.Key}/{NoneOf(byClass.Select(e => e.Method))}{trait}");
            }

            queries.Add($"/*/{byNamespace.Key}/{NoneOf(byNamespace.Select(e => e.Class))}/*{trait}");
        }

        queries.Add($"/*/{NoneOf(entries.Select(e => e.Namespace))}/*/*{trait}");
        return queries.SelectMany(q => (string[])["--filter-query", q]).ToList();
    }

    // NUnit drops every case of a parameterized method when it runs, but --list-tests still shows them. An escaped
    // "Method\(" clause would fix the listing, but it crashes NUnit's execution-time filter parser.
    public static List<string> VsTest(IReadOnlyList<QuarantineEntry> entries, string? userFilter)
    {
        var filter = string.Join("&", entries.Select(e => $"FullyQualifiedName!={e.FullName}"));
        return ["--filter", userFilter is null ? filter : $"({userFilter})&{filter}"];
    }

    private static string NoneOf(IEnumerable<string> names) => string.Join("&", names.Distinct().Select(n => $"(!{n})"));
}
