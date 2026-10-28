using System.Diagnostics;

namespace QuarantineCheck;

public sealed record AppResult(int ExitCode, string Output);

public static class TestApp
{
    public static AppResult Run(string app, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(app) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return new AppResult(process.ExitCode, output.Result + error.Result);
    }

    // Text --list-tests prints one display name per two-space-indented line before the summary; keep the method name.
    public static List<string> ListedMethods(string app, IEnumerable<string> arguments, out AppResult result)
    {
        result = Run(app, ["--list-tests", "--no-ansi", .. arguments]);
        var methods = new List<string>();
        foreach (var line in result.Output.Split('\n'))
        {
            if (line.StartsWith("Test discovery summary", StringComparison.Ordinal))
            {
                break;
            }

            if (line.StartsWith("  ", StringComparison.Ordinal) && !line.StartsWith("   ", StringComparison.Ordinal))
            {
                var name = line.Trim().Split('(')[0].Trim();
                methods.Add(name[(name.LastIndexOf('.') + 1)..]);
            }
        }

        return methods;
    }

    public static string Family(string app)
    {
        var help = Run(app, ["--help"]).Output;
        bool Has(string option) => help.Contains($"    {option}\n") || help.Contains($"    {option}\r\n");
        return Has("--filter-not-method") ? "xunit" : Has("--filter") ? "vstest" : Has("--treenode-filter") ? "treenode" : "none";
    }
}
