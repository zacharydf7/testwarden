namespace CrashLab;

// CRASHLAB_SCENARIO picks what goes wrong: clean, teardown-crash, midrun-crash or failure-teardown-crash.
public static class Scenario
{
    public static string Current => Environment.GetEnvironmentVariable("CRASHLAB_SCENARIO") ?? "clean";

    public static bool CrashesInTeardown => Current is "teardown-crash" or "failure-teardown-crash";

    public static bool CrashesMidRun => Current == "midrun-crash";

    public static bool FailsOneTest => Current == "failure-teardown-crash";
}
