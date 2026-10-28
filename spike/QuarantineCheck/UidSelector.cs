using System.Text.Json;

namespace QuarantineCheck;

// Framework-neutral: list the tests with the user's own filter, drop the quarantined methods, then select the rest
// by UID through a response file, which keeps the command line short whatever the test count.
public static class UidSelector
{
    public static List<string>? Select(string app, IReadOnlyList<QuarantineEntry> entries, IReadOnlyList<string> userArguments,
        string workDirectory)
    {
        var listing = TestApp.Run(app, ["--list-tests", "json", "--no-ansi", .. userArguments]);
        var start = listing.Output.IndexOf('{');
        if (listing.ExitCode != 0 || start < 0)
        {
            return null;
        }

        var quarantined = entries.Select(e => e.FullName).ToHashSet();
        var uids = new List<string>();
        using var document = JsonDocument.Parse(listing.Output[start..]);
        foreach (var test in document.RootElement.GetProperty("tests").EnumerateArray())
        {
            var type = test.GetProperty("type");
            var fullName = $"{type.GetProperty("namespace").GetString()}.{type.GetProperty("typeName").GetString()}."
                + type.GetProperty("methodName").GetString();
            if (!quarantined.Contains(fullName))
            {
                uids.Add(test.GetProperty("uid").GetString()!);
            }
        }

        var responseFile = Path.Combine(workDirectory, $"uids-{Guid.NewGuid():N}.rsp");
        File.WriteAllLines(responseFile, uids.Select(u => $"--filter-uid \"{u}\""));
        return ["@" + responseFile];
    }
}
