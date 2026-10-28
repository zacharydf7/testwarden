using Microsoft.Testing.Platform.Extensions.TestHost;

namespace Probe;

// Runs in the test host once the session is over, before any process-exit code gets a chance to crash.
public sealed class MarkerWriter(RunTally tally) : ITestHostApplicationLifetime
{
    public string Uid => "Probe.MarkerWriter";

    public string Version => "0.0.1";

    public string DisplayName => "Probe marker writer";

    public string Description => "Writes run.json when the test session finishes.";

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Task BeforeRunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task AfterRunAsync(int exitCode, CancellationToken cancellation)
    {
        var passed = tally.Count("passed");
        var failed = tally.Count("failed");
        var skipped = tally.Count("skipped");
        EvidenceFiles.Write(EvidenceFiles.RunFileName, new RunMarker(exitCode, passed, failed, skipped, passed + failed + skipped));
        return Task.CompletedTask;
    }
}
