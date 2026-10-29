using System.Text;
using System.Text.RegularExpressions;
using QuarantineCheck;

// Usage: QuarantineCheck <QuarantineLab dir> [--summary <file>]
// Checks that each strategy removes exactly the quarantined tests and keeps the user's own filter, in listing and in a run.
var labRoot = args[0];
var summaryIndex = Array.IndexOf(args, "--summary");
var summaryPath = summaryIndex >= 0 ? args[summaryIndex + 1] : null;
var entries = QuarantineEntry.ReadBitbucketFile(Path.Combine(AppContext.BaseDirectory, "quarantine.json"));
var workDirectory = Directory.CreateTempSubdirectory("quarantine").FullName;

string[] keptWithoutFilter = ["Adds", "BillsLater", "Empties", "Refunds", "Ships"];
string[] keptWithFilter = ["Adds", "BillsLater", "Refunds", "Ships"];

(string Lab, string Strategy)[] cases =
[
    ("XunitV3", "xunit-simple"), ("XunitV3", "xunit-query"), ("XunitV3", "uid"), ("XunitV3Mtp25", "uid"),
    ("Xunit4", "xunit-simple"), ("Xunit4", "xunit-query"), ("Xunit4", "vstest"), ("Xunit4", "uid"),
    ("MSTest", "vstest"), ("MSTest", "uid"),
    ("NUnit", "vstest"), ("NUnit", "uid"),
    ("TUnit", "uid"),
];

var summary = new StringBuilder();
summary.AppendLine($"### S2 quarantine matrix ({(OperatingSystem.IsWindows() ? "Windows" : "Linux")})");
summary.AppendLine();
summary.AppendLine("| Framework | Detected family | Strategy | User filter | Listed | Ran | Listing | Run |");
summary.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
var failures = 0;
foreach (var (lab, strategy) in cases)
{
    var app = Path.Combine(labRoot, lab, "bin", "Release", "net10.0", $"QuarantineLab.{lab}{(OperatingSystem.IsWindows() ? ".exe" : "")}");
    var family = TestApp.Family(app);
    foreach (var withUserFilter in (bool[])[false, true])
    {
        var expected = withUserFilter ? keptWithFilter : keptWithoutFilter;
        string listed, ran, listingResult, runResult;
        if (strategy == "uid" && !TestApp.ListsJson(app))
        {
            (listed, ran, listingResult, runResult) = ("-", "-", "not supported", "not supported");
        }
        else if (Arguments(lab, strategy, withUserFilter, app) is not { } arguments)
        {
            (listed, ran, listingResult, runResult) = ("-", "-", "**FAIL** (listing failed)", "**FAIL**");
            failures += 2;
        }
        else
        {
            var methods = TestApp.ListedMethods(app, arguments, out var listing);
            methods.Sort(StringComparer.Ordinal);
            var run = TestApp.Run(app, ["--no-ansi", "--no-progress", .. arguments]);
            var total = Regex.Match(run.Output, @"total: (\d+)").Groups[1].Value;
            listed = string.Join(" ", methods);
            ran = total.Length == 0 ? $"exit {run.ExitCode}" : total;
            // NUnit's --list-tests ignores "!=" on a parameterized method that its run honors (see FilterTranslator.VsTest).
            var knownGap = lab == "NUnit" && strategy == "vstest";
            var expectedListing = knownGap ? [.. expected.Append("Rounds").Append("Rounds").Order(StringComparer.Ordinal)] : expected;
            var listingOk = listing.ExitCode == 0 && methods.SequenceEqual(expectedListing);
            var runOk = total == expected.Length.ToString();
            listingResult = !listingOk ? "**FAIL**" : knownGap ? "known NUnit gap: lists Rounds" : "pass";
            runResult = runOk ? "pass" : "**FAIL**";
            failures += (listingOk ? 0 : 1) + (runOk ? 0 : 1);
        }

        summary.AppendLine($"| {lab} | {family} | {strategy} | {(withUserFilter ? "Category != Slow" : "none")} | {listed} | {ran} "
            + $"| {listingResult} | {runResult} |");
    }
}

summary.AppendLine();
summary.AppendLine($"Expected: `{string.Join(" ", keptWithoutFilter)}` without a user filter, `{string.Join(" ", keptWithFilter)}` with one.");
summary.AppendLine();
Console.WriteLine(summary);
if (summaryPath is not null)
{
    File.AppendAllText(summaryPath, summary.ToString());
}

return failures == 0 ? 0 : 1;

List<string>? Arguments(string lab, string strategy, bool withUserFilter, string app)
{
    var nativeFilter = !withUserFilter ? [] : lab switch
    {
        "XunitV3" or "XunitV3Mtp25" or "Xunit4" => ["--filter-not-trait", "Category=Slow"],
        "TUnit" => ["--treenode-filter", "/*/*/*/*[Category!=Slow]"],
        _ => (List<string>)["--filter", "TestCategory!=Slow"],
    };
    return strategy switch
    {
        "xunit-simple" => FilterTranslator.XunitSimple(entries, nativeFilter),
        "xunit-query" => FilterTranslator.XunitQuery(entries, withUserFilter ? "Category!=Slow" : null),
        "vstest" => FilterTranslator.VsTest(entries, !withUserFilter ? null : lab == "Xunit4" ? "Category!=Slow" : "TestCategory!=Slow"),
        _ => UidSelector.Select(app, entries, nativeFilter, workDirectory),
    };
}
