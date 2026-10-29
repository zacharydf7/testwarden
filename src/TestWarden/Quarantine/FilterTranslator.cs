using System.Text.RegularExpressions;

namespace TestWarden.Quarantine;

public enum FilterFamily
{
    XunitSimple,
    XunitQuery,
    VsTest,
}

// The fallback when the test app can't list its tests as JSON: quarantine expressed in the framework's own filter,
// combined with the user's filter of the same family.
public static class FilterTranslator
{
    // The user's own filter decides the family, because xUnit refuses to mix families; without one, the app's help does.
    public static FilterFamily ChooseFamily(IReadOnlyList<FilterOption> userFilters, string appHelp)
    {
        var names = userFilters.Select(f => f.Name).ToHashSet();
        var unsupported = names.FirstOrDefault(n => n is "--treenode-filter" or "--filter-uid");
        if (unsupported is not null)
        {
            throw new QuarantineException($"Quarantine can't be combined with {unsupported} unless the test app runs Microsoft.Testing.Platform 2.3 or later.");
        }

        var simple = names.Overlaps(FilterArguments.XunitSimpleOptions);
        if ((simple ? 1 : 0) + (names.Contains("--filter-query") ? 1 : 0) + (names.Contains("--filter") ? 1 : 0) > 1)
        {
            throw new QuarantineException("Use one filter family: xUnit's simple filters, --filter-query or --filter.");
        }

        if (names.Contains("--filter-query"))
        {
            return FilterFamily.XunitQuery;
        }

        if (names.Contains("--filter"))
        {
            return FilterFamily.VsTest;
        }

        if (simple || HasOption(appHelp, "--filter-not-method"))
        {
            return FilterFamily.XunitSimple;
        }

        return HasOption(appHelp, "--filter")
            ? FilterFamily.VsTest
            : throw new QuarantineException("This test app has no filter that can skip quarantined tests. Microsoft.Testing.Platform 2.3 or later can.");
    }

    // Returns the filter arguments to pass in place of the user's own.
    public static List<string> Translate(FilterFamily family, IReadOnlyList<QuarantineEntry> entries, IReadOnlyList<FilterOption> userFilters)
    {
        if (entries.Count == 0)
        {
            return [.. userFilters.SelectMany(f => (string[])[f.Name, .. f.Values])];
        }

        var wildcard = entries.FirstOrDefault(e => e.HasWildcards);
        if (wildcard is not null)
        {
            throw new QuarantineException($"'{wildcard.FullName}' uses a wildcard, which needs Microsoft.Testing.Platform 2.3 or later.");
        }

        return family switch
        {
            FilterFamily.XunitSimple => [.. userFilters.SelectMany(f => (string[])[f.Name, .. f.Values]),
                .. entries.SelectMany(e => (string[])["--filter-not-method", e.FullName])],
            FilterFamily.VsTest => VsTest(entries, Values(userFilters, "--filter")),
            _ => XunitQuery(entries, Values(userFilters, "--filter-query")),
        };
    }

    public static bool HasOption(string help, string option) =>
        Regex.IsMatch(help, $@"^\s*{Regex.Escape(option)}(\s|$)", RegexOptions.Multiline);

    private static List<string> VsTest(IReadOnlyList<QuarantineEntry> entries, List<string> userFilters)
    {
        var clauses = userFilters.Select(f => $"({f})").Concat(entries.Select(e => $"FullyQualifiedName!={e.FullName}"));
        return ["--filter", string.Join("&", clauses)];
    }

    // A query can't negate across segments, and several queries are OR'ed. So "not Class.Method" becomes one query per
    // quarantined class without its methods, one per namespace without those classes, and one for every other namespace,
    // each combined with every query of the user's.
    private static List<string> XunitQuery(IReadOnlyList<QuarantineEntry> entries, List<string> userQueries)
    {
        var global = entries.FirstOrDefault(e => e.Namespace.Length == 0);
        if (global is not null)
        {
            throw new QuarantineException($"'{global.FullName}' has no namespace, which an xUnit query filter can't exclude.");
        }

        var exclusions = new List<string[]>();
        foreach (var byNamespace in entries.GroupBy(e => e.Namespace))
        {
            foreach (var byClass in byNamespace.GroupBy(e => e.ClassName))
            {
                exclusions.Add(["*", byNamespace.Key, byClass.Key, NoneOf(byClass.Select(e => e.MethodName))]);
            }

            exclusions.Add(["*", byNamespace.Key, NoneOf(byNamespace.Select(e => e.ClassName)), "*"]);
        }

        exclusions.Add(["*", NoneOf(entries.Select(e => e.Namespace)), "*", "*"]);

        var arguments = new List<string>();
        foreach (var userQuery in userQueries.DefaultIfEmpty("/"))
        {
            var (segments, trait) = ParseQuery(userQuery);
            foreach (var exclusion in exclusions)
            {
                var merged = segments.Zip(exclusion, Both);
                arguments.AddRange(["--filter-query", "/" + string.Join("/", merged) + (trait is null ? "" : "/" + trait)]);
            }
        }

        return arguments;
    }

    // "/assembly/namespace/class/method[trait]", where missing segments match everything. The trait is written back as
    // its own segment, "/.../[trait]": xUnit 3.2 and 4.0 match nothing when it's attached to a segment.
    private static (string[] Segments, string? Trait) ParseQuery(string query)
    {
        var parts = query.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        string? trait = null;
        var bracket = parts.Count > 0 ? parts[^1].IndexOf('[') : -1;
        if (bracket >= 0)
        {
            trait = parts[^1][bracket..];
            parts[^1] = parts[^1][..bracket];
            if (parts[^1].Length == 0)
            {
                parts.RemoveAt(parts.Count - 1);
            }
        }

        if (parts.Count > 4)
        {
            throw new QuarantineException($"'{query}' has more than four segments, so it isn't a valid xUnit query.");
        }

        return ([.. parts, .. Enumerable.Repeat("*", 4 - parts.Count)], trait);
    }

    private static string Both(string user, string ours) => user == "*" ? ours : ours == "*" || ours == user ? user : $"({user})&({ours})";

    private static string NoneOf(IEnumerable<string> names) => string.Join("&", names.Distinct().Select(n => $"(!{n})"));

    private static List<string> Values(IReadOnlyList<FilterOption> filters, string name) =>
        filters.Where(f => f.Name == name).SelectMany(f => f.Values).ToList();
}
