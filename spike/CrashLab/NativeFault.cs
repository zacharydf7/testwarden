using System.Runtime.InteropServices;

namespace CrashLab;

// A real fault in native code, the way a crashing native library takes the process down.
public static partial class NativeFault
{
    private const uint AccessViolation = 0xC0000005;
    private const uint NonContinuable = 1;

    public static void Raise()
    {
        if (OperatingSystem.IsWindows())
        {
            RaiseException(AccessViolation, NonContinuable, 0, IntPtr.Zero);
        }
        else
        {
            // kill(getpid(), SIGSEGV) is swallowed by the runtime's signal handler, so read a bad address in libc instead.
            StringLength(1);
        }
    }

    [LibraryImport("kernel32.dll")]
    private static partial void RaiseException(uint code, uint flags, uint argumentCount, IntPtr arguments);

    [LibraryImport("libc", EntryPoint = "strlen")]
    private static partial nint StringLength(nint text);
}
