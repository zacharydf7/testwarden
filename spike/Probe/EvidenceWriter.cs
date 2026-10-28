using Microsoft.Testing.Platform.Extensions.TestHostControllers;

namespace Probe;

// Runs in the controller process, which outlives the test host and sees its real exit code.
public sealed class EvidenceWriter : ITestHostProcessLifetimeHandler
{
    public string Uid => "Probe.EvidenceWriter";

    public string Version => "0.0.1";

    public string DisplayName => "Probe evidence writer";

    public string Description => "Writes evidence.json when the test host process exits.";

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Task BeforeTestHostProcessStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task OnTestHostProcessStartedAsync(ITestHostProcessInformation testHostProcessInformation,
        CancellationToken cancellation) => Task.CompletedTask;

    public Task OnTestHostProcessExitedAsync(ITestHostProcessInformation testHostProcessInformation,
        CancellationToken cancellation)
    {
        var marker = EvidenceFiles.Read<RunMarker>(EvidenceFiles.RunFileName);
        var evidence = new HostEvidence(testHostProcessInformation.ExitCode, testHostProcessInformation.HasExitedGracefully,
            testHostProcessInformation.PID, marker is not null);
        EvidenceFiles.Write(EvidenceFiles.EvidenceFileName, evidence);
        IgnoreTrick.Apply(marker, evidence);
        return Task.CompletedTask;
    }
}
