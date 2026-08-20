using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CUETools.Linux.App.Views;
using CUETools.Ripper.SCSI;
using CUETools.Wpf.Models;
using CUETools.Wpf.Services;
using CUETools.Wpf.ViewModels;
using Xunit;

namespace CUETools.Linux.Tests;

// The Drive & Read page. The view model's behavior (selection sync, busy
// gating, corrupt-calibration fail-closed) is covered by the fork's own
// suite; these prove the Avalonia head's wiring: the view materializes
// against real Details data, the lamps key off the capability booleans,
// and a machine with no optical drive gets the honest empty state.
public class DrivePageTests
{
    private sealed class ScriptedDrives : IDriveService
    {
        public List<char> Drives = new() { 'D' };
        public DriveDetails Details = new()
        {
            Valid = true,
            Model = "TEST-BURNER 9000",
            Firmware = "1.00",
            ProductRevision = "A1",
            ARName = "TEST     - BURNER 9000",
            CurrentProfile = "CD-DA",
            Offset = 6,
            OffsetKnown = true,
            CanReadCD = true,
            CanReadDVD = true,
            CanWriteCD = true,
            C2ErrorPointers = true,
            CdText = false,
            MaxReadKBps = 8467,
            MaxReadCdX = 48,
            MaxTransferBytes = 131072,
            SupportedProfiles = new[] { "CD-ROM", "CD-R" },
            Features = new[]
            {
                new DriveFeatureRow { Name = "Cd Read", Current = true, CodeHex = "0x001E" },
                new DriveFeatureRow { Name = "Power Management", Current = false, CodeHex = "0x0100" },
            },
        };

        public IReadOnlyList<char> GetDrives() => Drives;
        public event EventHandler? SelectedDriveChanged;
        public char SelectedDrive { get; set; } = 'D';
        public bool RipInProgress => false;
        public event EventHandler? RipInProgressChanged;
        public DriveDetails GetDriveDetails(char drive) => Details;
        public DiscInfo? ReadDisc(char drive, Action<string>? onStatus = null) => null;
        public DriveTrayState GetTrayState(char drive) => DriveTrayState.Unknown;
        public void OpenTray(char drive) { }
        public void CloseTray(char drive) { }
        public void RaiseSelectionChanged() => SelectedDriveChanged?.Invoke(this, EventArgs.Empty);
        public void SilenceUnused() => RipInProgressChanged?.Invoke(this, EventArgs.Empty);
    }

    private static CUETools.Wpf.Accuracy.DriveCalibrationService NewCalibration(out string dir)
    {
        dir = Path.Combine(Path.GetTempPath(), $"drivepage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return new CUETools.Wpf.Accuracy.DriveCalibrationService(
            new NullDiagnosticLog(),
            new CUETools.Wpf.Accuracy.DriveCalibrationStore(Path.Combine(dir, "cal.bin")));
    }

    private sealed class NullDiagnosticLog : IDiagnosticLog
    {
        public string LogPath => "/tmp/nowhere.log";
        public void Info(string category, string message) { }
        public void Warn(string category, string message) { }
        public void Error(string category, string message, Exception? ex = null) { }
        public void Redact(params string?[] sensitive) { }
    }

    [AvaloniaFact]
    public async Task ThePageMaterializesTheDetectedDriveIdentity()
    {
        var drives = new ScriptedDrives();
        var cal = NewCalibration(out string dir);
        try
        {
            var vm = new DriveViewModel(drives, cal);
            // detect is fire-and-forget from the ctor; wait for it to land
            for (int i = 0; i < 300 && !vm.HasDetails; i++) await Task.Delay(10);

            var window = new Window { Content = new DriveView { DataContext = vm } };
            window.Show();

            Assert.True(vm.HasDetails);
            Assert.Contains("TEST-BURNER 9000", vm.Status);
            Assert.Equal("D:", vm.DriveLetter);
            Assert.False(vm.HasCal);
            Assert.Equal("not calibrated", vm.CacheText);
            window.Close();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [AvaloniaFact]
    public void AMachineWithNoOpticalDriveGetsTheHonestEmptyState()
    {
        var drives = new ScriptedDrives { Drives = new List<char>() };
        var cal = NewCalibration(out string dir);
        try
        {
            var vm = new DriveViewModel(drives, cal);
            var window = new Window { Content = new DriveView { DataContext = vm } };
            window.Show();

            Assert.False(vm.HasDetails);
            Assert.Equal("no optical drive", vm.DriveLetter);
            window.Close();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
