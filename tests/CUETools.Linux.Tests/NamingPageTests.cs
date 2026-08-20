using System.ComponentModel;
using System.Linq;
using CUETools.Linux.App;
using CUETools.Processor;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// The Naming editor page. The engine's transforms have their own suite in the fork;
// these tests pin the PAGE contract: edits persist into AppSettings (the file itself
// writes once at exit, D-043), presets re-announce, palette insertion respects the
// caret, and the preview always renders the canned examples.
public class NamingPageTests
{
    private static (NamingViewModel vm, AppSettings app) Create(Func<RipViewModel?>? ripSource = null)
    {
        CUEConfig config = Composition.CreateDefaultConfig();
        var app = new AppSettings();
        return (new NamingViewModel(config, app, ripSource), app);
    }

    [Fact]
    public void TemplateEditPersistsIntoSettings()
    {
        var (vm, app) = Create();
        vm.Template = "%artist%/%album%/%title%";
        Assert.Equal("%artist%/%album%/%title%", app.NamingTemplate);
    }

    [Fact]
    public void SchemeToggleEditsPersistIntoSettings()
    {
        var (vm, app) = Create();
        bool flipped = !vm.ExtractFeatured;
        vm.ExtractFeatured = flipped;
        Assert.Equal(flipped, app.NamingExtractFeatured);
    }

    [Fact]
    public void PresetApplyReplacesTheSchemeAndReannouncesEveryBoundProperty()
    {
        var (vm, _) = Create();
        vm.Template = "something/custom";
        var announced = new List<string>();
        vm.PropertyChanged += (_, e) => announced.Add(e.PropertyName ?? "");

        vm.ApplyPreset("Simple");

        Assert.Equal("%artist%/%album%/%tracknumber% - %title%", vm.Template);
        Assert.False(vm.ExtractFeatured);
        // the picker changes several properties at once; every bound one must re-announce
        foreach (string p in new[] { nameof(vm.Template), nameof(vm.ExtractFeatured),
            nameof(vm.UnifySeparators), nameof(vm.HandleArticles), nameof(vm.StripIllegal),
            nameof(vm.ReleaseDescriptor) })
            Assert.Contains(p, announced);
    }

    [Fact]
    public void UnknownPresetNameChangesNothing()
    {
        var (vm, _) = Create();
        string before = vm.Template;
        vm.ApplyPreset("No Such Preset");
        Assert.Equal(before, vm.Template);
    }

    [Fact]
    public void InsertFieldLandsAtTheCaretAndReturnsThePositionAfterIt()
    {
        var (vm, _) = Create();
        vm.Template = "AB";

        int next = vm.InsertField("%title%", 1);

        Assert.Equal("A%title%B", vm.Template);
        Assert.Equal(1 + "%title%".Length, next);
    }

    [Fact]
    public void InsertFieldClampsAnOutOfRangeCaret()
    {
        var (vm, _) = Create();
        vm.Template = "X";

        int next = vm.InsertField("%year%", 999);

        Assert.Equal("X%year%", vm.Template);
        Assert.Equal("X%year%".Length, next);
    }

    [Fact]
    public void PreviewRendersTheCannedExamplesFromConstruction()
    {
        var (vm, _) = Create();
        Assert.NotEmpty(vm.Preview);
        Assert.All(vm.Preview, g => Assert.NotEmpty(g.Lines));
    }

    [Fact]
    public void NullRipSourceMeansExamplesOnlyAndNoTrayGroup()
    {
        var (vm, _) = Create(ripSource: () => null);
        vm.Refresh();   // post-construction refresh, as the page's attach handler does
        Assert.DoesNotContain(vm.Preview, g => g.Label.StartsWith("Disc in tray"));
        Assert.NotEmpty(vm.Preview);
    }

    [Fact]
    public void EditingTheTemplateRebuildsThePreviewLines()
    {
        var (vm, _) = Create();
        vm.Template = "FLAT/%title%";
        Assert.All(vm.Preview, g => Assert.All(g.Lines, l => Assert.StartsWith("FLAT/", l)));
    }
}
