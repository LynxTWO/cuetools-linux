namespace CUETools.Linux.Tests;

// The replay claim exists only where a cross-process exclusion primitive is
// wired: Windows (share mode) and Linux (advisory lock). macOS gets no claim
// and no replay rather than a silent race - the fail-closed choice
// VerificationBackfillService documents. Replay-behavior tests therefore run
// only where replay runs; what macOS does instead is pinned by
// MacReplayFailsClosedTests, so the advisory macos CI lane goes red the day
// someone wires a claim there without revisiting all of these.
internal static class ReplayPlatform
{
    public static bool Unsupported =>
        !OperatingSystem.IsWindows() && !OperatingSystem.IsLinux();
}
