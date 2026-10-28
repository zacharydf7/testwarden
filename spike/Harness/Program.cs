using System.Text;
using Harness;
using Probe;

// Usage: Harness <project dir> <label> <runs> <exe|dotnet-test> [--ignore-trick] [--summary <file>] [-- <app args>]
var projectDirectory = args[0];
var label = args[1];
var runs = int.Parse(args[2]);
var mode = args[3] == "exe" ? LaunchMode.Exe : LaunchMode.DotnetTest;
var ignoreTrick = args.Contains("--ignore-trick");
var summaryIndex = Array.IndexOf(args, "--summary");
var summaryPath = summaryIndex >= 0 ? args[summaryIndex + 1] : null;
var separator = Array.IndexOf(args, "--");
var extraArguments = separator >= 0 ? args[(separator + 1)..] : [];

string[] scenarios = ["clean", "teardown-crash", "midrun-crash", "failure-teardown-crash"];
var root = Path.Combine(Path.GetTempPath(), "crashlab", Guid.NewGuid().ToString("N"));
var observations = new List<Observation>();
foreach (var scenario in scenarios)
{
    for (var run = 1; run <= runs; run++)
    {
        var directory = Path.Combine(root, scenario, run.ToString());
        Directory.CreateDirectory(directory);
        Environment.SetEnvironmentVariable("TESTWARDEN_RESULTS_DIRECTORY", directory);
        var exitCode = Launcher.Run(mode, projectDirectory, scenario, directory, extraArguments, ignoreTrick);
        var junitPath = Path.Combine(directory, "junit.xml");
        var observation = new Observation(scenario, run, exitCode, EvidenceFiles.Read<HostEvidence>(EvidenceFiles.EvidenceFileName),
            EvidenceFiles.Read<RunMarker>(EvidenceFiles.RunFileName), File.Exists(junitPath), JunitReport.ProvesCleanRun(junitPath),
            CoverageReport.CoversSampleLibrary(directory), Directory.GetFiles(directory, "*.dmp").Length > 0);
        observations.Add(observation);
        Console.WriteLine($"{label} {scenario} #{run}: exit {exitCode}, host {observation.Evidence?.ExitCode.ToString() ?? "-"}, "
            + $"verdict {VerdictRules.Decide(observation)}");
    }
}

var summary = new StringBuilder();
summary.AppendLine($"### {label} ({mode}, {runs} runs, {(OperatingSystem.IsWindows() ? "Windows" : "Linux")})");
summary.AppendLine();
summary.AppendLine("| Scenario | Exit codes | Host exit codes | Graceful | run.json | JUnit proof | Coverage | Dump | Verdicts | OK |");
summary.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
var mismatches = 0;
foreach (var scenario in scenarios)
{
    var group = observations.Where(o => o.Scenario == scenario).ToList();
    var wrong = group.Count(o => !VerdictRules.Matches(scenario, VerdictRules.Decide(o)));
    mismatches += wrong;
    var ok = wrong == 0 ? "yes" : $"**{wrong} wrong**";
    summary.AppendLine($"| {scenario} | {Tally(group, o => o.ExitCode.ToString())} "
        + $"| {Tally(group, o => o.Evidence?.ExitCode.ToString() ?? "none")} "
        + $"| {Tally(group, o => o.Evidence?.HasExitedGracefully.ToString() ?? "none")} "
        + $"| {Tally(group, o => o.Marker is null ? "no" : "yes")} "
        + $"| {Tally(group, o => o.JunitProvesCleanRun ? "yes" : o.JunitExists ? "no" : "missing")} "
        + $"| {Tally(group, o => o.CoverageExists ? "yes" : "no")} | {Tally(group, o => o.DumpExists ? "yes" : "no")} "
        + $"| {Tally(group, o => VerdictRules.Decide(o).ToString())} | {ok} |");
}

summary.AppendLine();
Console.WriteLine(summary);
if (summaryPath is not null)
{
    File.AppendAllText(summaryPath, summary.ToString());
}

return mismatches == 0 ? 0 : 1;

static string Tally(List<Observation> group, Func<Observation, string> key) =>
    string.Join(", ", group.GroupBy(key).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} x{g.Count()}"));
