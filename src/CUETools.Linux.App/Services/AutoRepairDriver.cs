using System.ComponentModel;
using CUETools.Wpf.ViewModels;

namespace CUETools.Linux.App.Services;

/// <summary>
/// Drives repairs automatically for the --repair launch flag: whenever the
/// verify workspace goes idle with results, the next repairable disc is
/// repaired, until none remain. The flag is explicit user consent, so the
/// composition pairs this driver with an auto-confirming prompt; the
/// interactive confirmation exists to guard stray clicks, not scripted
/// instructions. Repairs run one at a time (the view model's IsBusy gate).
/// Each disc gets exactly one attempt: a failed repair leaves the disc
/// repairable, and retrying a deterministic failure forever rebuilds the
/// sibling copy in a loop (observed live before this guard existed).
/// </summary>
public sealed class AutoRepairDriver
{
    private readonly VerifyViewModel _verify;
    private readonly Action<string>? _log;
    private readonly HashSet<VerifyDiscViewModel> _attempted = new();

    public AutoRepairDriver(VerifyViewModel verify, Action<string>? log = null)
    {
        _verify = verify;
        _log = log;
        _verify.PropertyChanged += OnVerifyPropertyChanged;
    }

    public int RepairsStarted { get; private set; }

    private void OnVerifyPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(VerifyViewModel.IsBusy) ||
            _verify.IsBusy || !_verify.HasResult)
        {
            return;
        }

        VerifyDiscViewModel? next = null;
        foreach (VerifyDiscViewModel disc in _verify.Discs)
        {
            if (!disc.CanRepair)
            {
                continue;
            }

            if (_attempted.Contains(disc))
            {
                _log?.Invoke(
                    $"--repair: {disc.DisplayName} is still repairable after its attempt; not retrying");
                continue;
            }

            next = disc;
            break;
        }

        if (next == null)
        {
            return;
        }

        _attempted.Add(next);
        RepairsStarted++;
        _log?.Invoke($"--repair: starting repair {RepairsStarted} ({next.DisplayName})");
        _verify.RepairDiscCommand.Execute(next);
    }
}

/// <summary>Auto-confirming prompt used only with the --repair flag.</summary>
public sealed class AutoConfirmPrompt : CUETools.Wpf.Services.IUserPrompt
{
    public Task<bool> ConfirmOkCancelAsync(string message, string title)
        => Task.FromResult(true);
}
