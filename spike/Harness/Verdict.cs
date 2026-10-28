namespace Harness;

public enum Verdict
{
    Pass,
    PassForgiven,
    FailTests,
    FailCrash,
    FailOther,
}

// The prototype of the tool's verdict rules, with --forgive-teardown-crash on.
public static class VerdictRules
{
    public static Verdict Decide(Observation observation)
    {
        var crashed = IsCrash(observation.ExitCode)
            || observation.Evidence is { HasExitedGracefully: false }
            || (observation.Evidence is not null && IsCrash(observation.Evidence.ExitCode));
        if (!crashed)
        {
            return observation.ExitCode switch
            {
                0 => Verdict.Pass,
                2 => Verdict.FailTests,
                _ => Verdict.FailOther,
            };
        }

        if (observation.Marker is { Failed: > 0 } || observation.ExitCode == 2)
        {
            return Verdict.FailTests;
        }

        var markerProof = observation.Marker is { ExitCode: 0, Failed: 0, Total: > 0 };
        return markerProof || observation.JunitProvesCleanRun ? Verdict.PassForgiven : Verdict.FailCrash;
    }

    public static bool Matches(string scenario, Verdict verdict) => scenario switch
    {
        "clean" => verdict == Verdict.Pass,
        "teardown-crash" => verdict == Verdict.PassForgiven,
        _ => verdict is Verdict.FailTests or Verdict.FailCrash or Verdict.FailOther,
    };

    // 7 is MTP's TestHostProcessExitedNonGracefully; signals show as 128 + n on Linux and NTSTATUS codes are negative.
    private static bool IsCrash(int exitCode) =>
        exitCode == 7 || (OperatingSystem.IsWindows() ? exitCode < 0 : exitCode > 128);
}
