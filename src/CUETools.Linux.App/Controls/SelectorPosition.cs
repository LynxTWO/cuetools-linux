using System.Globalization;
using Avalonia.Data.Converters;

namespace CUETools.Linux.App.Controls;

/// <summary>
/// The position readout under a machined selector window's value: "3 / 7".
/// A tape counter, and it earns its space the same way one does - by saying
/// how much else is in there without opening anything. On a drive list that
/// is a real signal; the readout is opt-in per site (the "counted" class)
/// rather than global, because on a fixed three-item list it is noise.
///
/// Silent by design when it has nothing true to say: no selection, an empty
/// list, or a list of one all render as empty rather than as "0 / 0" or a
/// counter that never moves.
/// </summary>
public sealed class SelectorPositionConverter : IMultiValueConverter
{
    public static readonly SelectorPositionConverter Instance = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 ||
            values[0] is not int index ||
            values[1] is not int count ||
            count <= 1 ||
            index < 0 ||
            index >= count)
            return string.Empty;

        return $"{index + 1} / {count}";
    }
}
