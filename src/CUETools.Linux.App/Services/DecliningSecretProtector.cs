using CUETools.Wpf.Services;

namespace CUETools.Linux.App.Services;

/// <summary>
/// The Linux head has no protected credential storage yet (DPAPI is the
/// Windows head's answer), so secrets are simply never persisted: Protect
/// returns empty, which the store treats as nothing-to-save, and Unprotect
/// of a foreign protected value declines, which the store treats as no
/// credential until the user sets it again. Honest over convenient - a
/// plaintext fallback in the profile is not an option.
/// </summary>
public sealed class DecliningSecretProtector : ISecretProtector
{
    public string Protect(string secret) => "";

    public string Unprotect(string protectedValue)
        => throw new NotSupportedException(
            "Protected credentials are not supported on this platform yet.");
}
