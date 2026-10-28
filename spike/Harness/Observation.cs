using Probe;

namespace Harness;

public sealed record Observation(string Scenario, int Run, int ExitCode, HostEvidence? Evidence, RunMarker? Marker,
    bool JunitExists, bool JunitProvesCleanRun, bool CoverageExists, bool DumpExists);
