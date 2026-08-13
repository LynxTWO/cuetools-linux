#if RIP_DIAGNOSTIC
using CUETools.Ripper;
using CUETools.Ripper.SCSI;
using CUETools.Wpf.Accuracy;

namespace CUETools.Linux.App.Services;

/// <summary>
/// Dev-only rip transport diagnostic (D-053, SLICE-009 increment 1):
/// enumerates optical drives and proves INQUIRY plus READ TOC through the
/// fork's Linux SG_IO transport. Prints drive identity and TOC geometry
/// only - never album or artist text (scrubbed-logging rule). The whole
/// file is compiled out of Release builds; the flag does not exist there.
/// </summary>
internal static class RipDiagnostic
{
    /// <summary>Runs the probe; returns the number of failed drives.</summary>
    internal static int Run()
    {
        char[] drives = CDDrivesList.DrivesAvailable();
        Console.WriteLine($"rip-diagnostic: {drives.Length} drive(s) enumerated");
        int failures = 0;
        foreach (char letter in drives)
        {
            string node = Bwg.Scsi.LinuxSg.DevicePathForLetter(letter) ?? "(no device node)";
            try
            {
                var reader = new CDDriveReader();
                try
                {
                    if (!reader.Open(letter))
                    {
                        Console.WriteLine($"  {letter} {node} open returned false");
                        failures++;
                        continue;
                    }
                    Console.WriteLine(
                        $"  {letter} {node} identity=[{reader.ARName}] " +
                        $"tracks={reader.TOC.TrackCount} audioSectors={reader.TOC.AudioLength}");

                    // Increment 2: run the fork's read-command matrix probe
                    // (BEh/D8h x C2 modes x main-channel modes) - real READ CD
                    // payload transfers through the transport. The probe
                    // decides drive capability; its result string is the
                    // evidence.
                    string detect = reader.AutoDetectReadCommand;
                    Console.WriteLine($"    read-command: {reader.CurrentReadCommand}");
                    foreach (string line in detect.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        Console.WriteLine($"    probe: {line.TrimEnd()}");

                    // Drive read offset from the AccurateRip drive table (the
                    // same HTTPS-fetched, 10-day-cached DriveOffsets.bin the
                    // WPF head uses). Lookup evidence only; the rip flow
                    // applies it in a later increment.
                    bool known = CUETools.AccurateRip.AccurateRipVerify
                        .FindDriveReadOffset(reader.ARName, out int arOffset);
                    Console.WriteLine(known
                        ? $"    ar-offset: {arOffset:+0;-0;+0} samples (AccurateRip drive table)"
                        : "    ar-offset: not found in the AccurateRip drive table");

                    // Secure-window read: one Read pulls a whole PrefetchSector
                    // window (MSECTORS = 2400 sectors, ~32 s of audio) through
                    // the engine's multi-pass vote path at the default
                    // correction quality. Stats only - sector payloads are
                    // never printed (scrubbed-logging rule).
                    int maxPass = 0, progressEvents = 0;
                    EventHandler<ReadProgressArgs> onProgress = (_, args) =>
                    {
                        progressEvents++;
                        if (args.Pass > maxPass)
                            maxPass = args.Pass;
                    };
                    reader.ReadProgress += onProgress;
                    try
                    {
                        var buff = new CUETools.Codecs.AudioBuffer(
                            CUETools.Codecs.AudioPCMConfig.RedBook, 588 * 100);
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        int got = reader.Read(buff, -1);
                        sw.Stop();
                        int failed = 0;
                        foreach (bool bit in reader.FailedSectors)
                            if (bit)
                                failed++;
                        Console.WriteLine(
                            $"    secure-read: {got} samples ({sw.ElapsedMilliseconds} ms window), " +
                            $"passes={maxPass + 1}, events={progressEvents}, failedSectors={failed}, " +
                            $"cacheDefeatBytes={reader.CacheDefeatBytes}, " +
                            $"commRetries={reader.ReadCommunicationRetryCount}");
                    }
                    finally
                    {
                        reader.ReadProgress -= onProgress;
                    }
                }
                finally
                {
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {letter} {node} FAILED {ex.GetType().Name}: {ex.Message}");
                failures++;
            }

            // Increment 3: the real calibration transaction (the same
            // DriveCalibrationService the WPF head runs before secure work) -
            // cache-behavior probe, cache-defeat search, speed range, and
            // lead-in/out probing, persisted to a diagnostic-scratch store so
            // the future app-owned store stays untouched.
            try
            {
                string calDir = Path.Combine(Path.GetTempPath(),
                    "cuetools-rip-diagnostic-cal");
                Directory.CreateDirectory(calDir);
                var calService = new DriveCalibrationService(
                    new ConsoleDiagLog(),
                    new DriveCalibrationStore(Path.Combine(calDir, "calibration.json.gz")));
                DriveCalibration? cal = calService.Calibrate(letter);
                if (cal == null)
                {
                    Console.WriteLine($"    calibration: returned null (drive busy or no audio disc)");
                    failures++;
                }
                else
                {
                    Console.WriteLine(
                        $"    calibration: cacheDefeat={cal.CacheDefeat} ({cal.CacheConfidence}), " +
                        $"offset={cal.ReadOffsetSamples:+0;-0;+0} (known={cal.ReadOffsetKnown}), " +
                        $"overread in={cal.OverreadLeadIn} out={cal.OverreadLeadOut}, " +
                        $"speed {cal.MinSpeedKbps}-{cal.MaxSpeedKbps} kbps, version={cal.RipperVersion}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    calibration FAILED {ex.GetType().Name}: {ex.Message}");
                failures++;
            }
        }
        return failures;
    }

    /// <summary>Console sink for the calibration service's diagnostic log.
    /// The diagnostic prints hardware metadata only, so Redact is a no-op
    /// here; the app-owned DiagnosticLog handles real jobs.</summary>
    private sealed class ConsoleDiagLog : CUETools.Wpf.Services.IDiagnosticLog
    {
        public void Info(string area, string message) =>
            Console.WriteLine($"    [{area}] {message}");
        public void Warn(string area, string message) =>
            Console.WriteLine($"    [{area}] WARN {message}");
        public void Error(string area, string message, Exception? ex = null) =>
            Console.WriteLine($"    [{area}] ERROR {message}" +
                (ex == null ? "" : $" ({ex.GetType().Name})"));
        public void Redact(params string?[] sensitive) { }
        public string LogPath => "(console)";
    }
}
#endif
