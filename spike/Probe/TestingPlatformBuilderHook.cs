using Microsoft.Testing.Platform.Builder;

namespace Probe;

public static class TestingPlatformBuilderHook
{
    public static void AddExtensions(ITestApplicationBuilder builder, string[] _)
    {
        var tally = new RunTally();
        builder.TestHost.AddDataConsumer(_ => new OutcomeCounter(tally));
        builder.TestHost.AddTestHostApplicationLifetime(_ => new MarkerWriter(tally));
        builder.TestHostControllers.AddProcessLifetimeHandler(_ => new EvidenceWriter());
    }
}
