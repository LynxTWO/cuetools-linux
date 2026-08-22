using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace CUETools.Linux.App.Controls;

internal static class VisualDescendantExtensions
{
    /// <summary>Find a named element inside a control's applied template. The
    /// template may not be applied yet on the first call, which is why every
    /// caller must tolerate null rather than assume.</summary>
    public static Control? GetVisualDescendant(this Control root, string name)
    {
        foreach (Visual v in root.GetVisualDescendants())
            if (v is Control c && c.Name == name)
                return c;
        return null;
    }
}
