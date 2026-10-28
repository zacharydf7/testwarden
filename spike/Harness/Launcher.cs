using System.Diagnostics;

namespace Harness;

public enum LaunchMode
{
    Exe,
    DotnetTest,
}

public static class Launcher
{
    public static int Run(LaunchMode mode, string projectDirectory, string scenario, string resultsDirectory,
        IReadOnlyList<string> extraArguments, bool ignoreTrick)
    {
        var start = mode == LaunchMode.Exe ? ExeStart(projectDirectory) : DotnetTestStart(projectDirectory);
        string[] reportArguments = ["--no-progress", "--no-ansi", "--results-directory", resultsDirectory, "--report-junit",
            "--report-junit-filename", "junit.xml"];
        foreach (var argument in reportArguments.Concat(extraArguments))
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["CRASHLAB_SCENARIO"] = scenario;
        start.Environment["TESTWARDEN_RESULTS_DIRECTORY"] = resultsDirectory;
        start.Environment["PROBE_IGNORE_TRICK"] = ignoreTrick ? "1" : "0";
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;

        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        File.WriteAllText(Path.Combine(resultsDirectory, "console.log"), output.Result + error.Result);
        return process.ExitCode;
    }

    private static ProcessStartInfo ExeStart(string projectDirectory)
    {
        var name = Path.GetFileName(Path.GetFullPath(projectDirectory).TrimEnd(Path.DirectorySeparatorChar, '/'));
        var extension = OperatingSystem.IsWindows() ? ".exe" : "";
        return new ProcessStartInfo(Path.Combine(projectDirectory, "bin", "Release", "net10.0", name + extension));
    }

    private static ProcessStartInfo DotnetTestStart(string projectDirectory) =>
        new("dotnet") { ArgumentList = { "test", "--project", projectDirectory, "--no-build", "-c", "Release" } };
}
