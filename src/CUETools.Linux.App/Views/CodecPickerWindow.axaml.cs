using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CUETools.Wpf.Services;

namespace CUETools.Linux.App.Views;

/// <summary>
/// Codec picker: every implementation the build knows, healthy rows
/// selectable, unavailable rows explanatory and disabled (the WPF
/// CodecPickerWindow's core; its sort modes are a logged follow-up).
/// ShowDialog result is the chosen CodecChoice, or null on cancel.
/// </summary>
public partial class CodecPickerWindow : Window
{
    public CodecPickerWindow()
    {
        InitializeComponent();
    }

    public CodecPickerWindow(IEnumerable<CodecChoice> choices, string? selectedStableId)
        : this()
    {
        var items = choices.ToList();
        ChoiceList.ItemsSource = items;
        ChoiceList.SelectedItem = items.FirstOrDefault(choice =>
            selectedStableId != null &&
            string.Equals(choice.StableId, selectedStableId,
                System.StringComparison.OrdinalIgnoreCase) &&
            choice.CanSelect);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
        => Close(ChoiceList.SelectedItem as CodecChoice);

    private void OnCancelClick(object? sender, RoutedEventArgs e)
        => Close(null);
}
