namespace Harness;

// What each spike asks of a run, on top of the verdict, so a job only goes green when its own question is answered.
public static class Expectations
{
    public static bool Met(Observation observation, bool ignoreTrick, IReadOnlyList<string> appArguments)
    {
        var scenario = observation.Scenario;
        if (!VerdictRules.Matches(scenario, VerdictRules.Decide(observation)))
        {
            return false;
        }

        // With the probe on, forgiveness must come from its marker, not from the JUnit fallback.
        if (observation.Evidence is not null && scenario == "teardown-crash" && observation.Marker is null)
        {
            return false;
        }

        // S4: the controller itself must turn a proven teardown crash into 0 and leave every other crash failing.
        if (ignoreTrick && (scenario == "teardown-crash" ? observation.ExitCode != 0 : scenario != "clean" && observation.ExitCode == 0))
        {
            return false;
        }

        // S3: a crash in teardown can stop coverlet's own end-of-process flush, so only the clean run must have coverage.
        if (appArguments.Contains("--coverlet") && scenario == "clean" && !observation.CoverageExists)
        {
            return false;
        }

        return !appArguments.Contains("--crashdump") || scenario == "clean" || observation.DumpExists;
    }
}
