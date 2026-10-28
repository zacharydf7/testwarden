namespace Probe;

// S4: can the controller forgive a proven teardown crash itself, by asking MTP to ignore exit code 7?
public static class IgnoreTrick
{
    public static void Apply(RunMarker? marker, HostEvidence evidence)
    {
        if (Environment.GetEnvironmentVariable("PROBE_IGNORE_TRICK") != "1")
        {
            return;
        }

        var provenClean = marker is { ExitCode: 0, Failed: 0, Total: > 0 };
        if (provenClean && evidence.ExitCode != 0)
        {
            Environment.SetEnvironmentVariable("TESTINGPLATFORM_EXITCODE_IGNORE", "7");
        }
    }
}
