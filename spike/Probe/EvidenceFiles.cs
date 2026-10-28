using System.Text.Json;

namespace Probe;

public sealed record RunMarker(int ExitCode, int Passed, int Failed, int Skipped, int Total);

public sealed record HostEvidence(int ExitCode, bool HasExitedGracefully, int Pid, bool RunFileExisted);

public static class EvidenceFiles
{
    public const string RunFileName = "run.json";
    public const string EvidenceFileName = "evidence.json";

    private static string Directory =>
        Environment.GetEnvironmentVariable("TESTWARDEN_RESULTS_DIRECTORY") ?? Path.Combine(AppContext.BaseDirectory, "TestResults");

    // Write then rename, so a crash mid-write leaves no file rather than half a file.
    public static void Write<T>(string fileName, T value)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var path = Path.Combine(Directory, fileName);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value));
        File.Move(temporaryPath, path, overwrite: true);
    }

    public static T? Read<T>(string fileName)
    {
        var path = Path.Combine(Directory, fileName);
        return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path)) : default;
    }
}
