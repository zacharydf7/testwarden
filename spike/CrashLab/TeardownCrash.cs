[assembly: AssemblyFixture(typeof(CrashLab.TeardownCrash))]

namespace CrashLab;

// An assembly fixture only exists in the test host, never in the controller process that launches it.
public sealed class TeardownCrash
{
    public TeardownCrash()
    {
        if (Scenario.CrashesInTeardown)
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) => NativeFault.Raise();
        }
    }
}
