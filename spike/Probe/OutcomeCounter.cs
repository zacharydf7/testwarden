using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace Probe;

public sealed class OutcomeCounter(RunTally tally) : IDataConsumer
{
    public Type[] DataTypesConsumed => [typeof(TestNodeUpdateMessage)];

    public string Uid => "Probe.OutcomeCounter";

    public string Version => "0.0.1";

    public string DisplayName => "Probe outcome counter";

    public string Description => "Counts test outcomes for the end-of-run marker.";

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        var node = ((TestNodeUpdateMessage)value).TestNode;
        var outcome = node.Properties.SingleOrDefault<TestNodeStateProperty>() switch
        {
            PassedTestNodeStateProperty => "passed",
            SkippedTestNodeStateProperty => "skipped",
            FailedTestNodeStateProperty or ErrorTestNodeStateProperty or TimeoutTestNodeStateProperty
                or CancelledTestNodeStateProperty => "failed",
            _ => null,
        };
        if (outcome is not null)
        {
            tally.Record(node.Uid.Value, outcome);
        }

        return Task.CompletedTask;
    }
}
