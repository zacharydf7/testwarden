namespace TestWarden.Quarantine;

public sealed record FilterOption(string Name, IReadOnlyList<string> Values);

// Separates the user's own filter options from the rest of the test app arguments.
public sealed record FilterArguments(IReadOnlyList<FilterOption> Filters, IReadOnlyList<string> Rest)
{
    public static readonly string[] XunitSimpleOptions =
    [
        "--filter-class", "--filter-not-class", "--filter-method", "--filter-not-method", "--filter-namespace",
        "--filter-not-namespace", "--filter-trait", "--filter-not-trait", "--filter-display-name", "--filter-not-display-name",
    ];

    public static readonly string[] OtherOptions = ["--filter-query", "--filter", "--treenode-filter", "--filter-uid"];

    // An option's values run up to the next argument that starts with "--".
    public static FilterArguments Split(IReadOnlyList<string> arguments)
    {
        var filters = new List<FilterOption>();
        var rest = new List<string>();
        for (var i = 0; i < arguments.Count; i++)
        {
            if (!XunitSimpleOptions.Contains(arguments[i]) && !OtherOptions.Contains(arguments[i]))
            {
                rest.Add(arguments[i]);
                continue;
            }

            var values = new List<string>();
            while (i + 1 < arguments.Count && !arguments[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values.Add(arguments[++i]);
            }

            filters.Add(new FilterOption(arguments[i - values.Count], values));
        }

        return new FilterArguments(filters, rest);
    }
}
