#if RIP_DIAGNOSTIC
using CUETools.Ripper;
using CUETools.Ripper.SCSI;
using CUETools.Wpf;
using CUETools.Wpf.Accuracy;
using CUETools.Wpf.Services;

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

    /// <summary>
    /// Increment 4 evidence run: the full secure Test &amp; Copy transaction
    /// (RipService.RunTestAndCopy - calibration prerequisite, cache-defeated
    /// Test read, staged Copy read, checksum resolution, phase evidence, and
    /// commit or held state) against one drive, into a scratch output
    /// directory. Dev-only like everything in this file (D-053).
    /// </summary>
    internal static int RunTestCopy(char letter)
    {
        Composition.RegisterManagedCodecs();
        var nativeLines = new List<string>();
        var loader = Composition.RegisterNativeCodecs(nativeLines.Add);
        var log = new ConsoleDiagLog();
        foreach (string line in nativeLines)
            log.Info("codecs", line);

        var config = Composition.CreateDefaultConfig();
        var app = new CUETools.Wpf.Services.AppSettings();
        // Empty packaged-hash table: curated CLI encoders stay unavailable,
        // which is fine - this evidence run encodes FLAC through the
        // hash-validated native codec.
        var catalog = new EncoderCatalog(
            log, app,
            Path.Combine(AppContext.BaseDirectory, "encoders"),
            new Dictionary<string, string>());
        catalog.EnsureRegistered(config);

        string scratchRoot = Path.Combine(Path.GetTempPath(), "cuetools-rip-tc");
        Directory.CreateDirectory(scratchRoot);
        string calDir = Path.Combine(Path.GetTempPath(), "cuetools-rip-diagnostic-cal");
        Directory.CreateDirectory(calDir);
        var calStore = new DriveCalibrationStore(Path.Combine(calDir, "calibration.json.gz"));
        var history = new VerifyHistoryStore(Path.Combine(calDir, "verify-history.json.gz"));
        var calService = new DriveCalibrationService(log, calStore);
        var rip = new RipService(config, log, app, catalog, calStore, history, calService);

        Console.WriteLine($"rip-tc: drive {letter} -> {scratchRoot} (secure Test & Copy, FLAC)");
        double lastFrac = -1;
        string lastMsg = "";
        var result = rip.RunTestAndCopy(
            letter,
            cq: 1,
            format: "flac",
            metadata: null,
            outputBaseDir: scratchRoot,
            onProgress: (frac, msg) =>
            {
                // Console throttle: whole-percent steps or a phase change.
                if (msg != lastMsg || frac - lastFrac >= 0.05 || frac >= 1.0)
                {
                    lastFrac = frac;
                    lastMsg = msg;
                    Console.WriteLine($"  [{frac,6:P1}] {msg}");
                }
            },
            onReadVerdict: v => Console.WriteLine(
                $"  verdict[{v.ReadIndex}:{v.ReadKind}] AR {v.ArConfidence}/{v.ArTotal}, " +
                $"CTDB {v.CtdbConfidence}/{v.CtdbTotal}, accurate={v.Accurate}"),
            onCrcEvidence: crcs => Console.WriteLine(
                $"  crc-evidence: {crcs.Length} track(s) published"));

        Console.WriteLine($"rip-tc result: ok={result.Ok} outcome={result.Outcome} readsUsed={result.ReadsUsed}");
        if (!result.Ok)
            Console.WriteLine($"  error: {result.Error}");
        if (result.HoldReason.Length != 0 || result.HeldTracks.Length != 0)
            Console.WriteLine($"  HELD: reason=[{result.HoldReason}] tracks=[{string.Join(",", result.HeldTracks)}] staging={result.CopyStagingDir}");
        if (result.OutputDir.Length != 0)
            Console.WriteLine($"  output: {result.FileCount} file(s) in {result.OutputDir}");
        Console.WriteLine(
            $"  databases: AR {result.ArConfidence}/{result.ArTotal}, CTDB {result.CtdbConfidence}/{result.CtdbTotal}, " +
            $"accurate={result.Accurate}, failedWindows={result.FailedWindows}");
        Console.WriteLine(
            $"  history: recorded={result.HistoryRecorded} known={result.HistoryKnown} matches={result.HistoryMatches} priorReads={result.HistoryPriorReads}");
        return result.Ok ? 0 : 1;
    }

    /// <summary>
    /// Raw BEh read-shape probe (dev-only): issues cache-defeat-shaped reads
    /// with varying sector counts at fixed LBAs to characterize a drive's
    /// response to partial chunks. Prints status and sense per shape.
    /// </summary>
    internal static int RunReadShapeProbe(char letter)
    {
        var logger = new Bwg.Logging.Logger();
        var reader = new CDDriveReader();
        if (!reader.Open(letter))
        {
            Console.WriteLine("open failed");
            return 1;
        }
        uint firstAudio = reader.TOC[reader.TOC.FirstAudio][0].Start;
        uint audioLen = reader.TOC.AudioLength;
        string detect = reader.AutoDetectReadCommand;
        Console.WriteLine($"probe drive {letter}: [{reader.ARName}] audioLen={audioLen} readCmd={reader.CurrentReadCommand}");
        reader.Close();

        var dev = new Bwg.Scsi.Device(logger);
        if (!dev.Open(letter))
        {
            Console.WriteLine("raw device open failed");
            return 1;
        }
        try
        {
            int[] counts = { 16, 15, 14, 10, 8, 4, 2, 1, 16 };
            uint[] bases = { audioLen / 10, audioLen / 2, audioLen * 8 / 10 };
            const int perSector = 4 * 588 + 294;
            IntPtr scratch = System.Runtime.InteropServices.Marshal.AllocHGlobal(16 * perSector);
            try
            {
                foreach (uint rel in bases)
                {
                    foreach (int count in counts)
                    {
                        uint lba = firstAudio + rel;
                        var st = dev.ReadCDAndSubChannel(
                            Bwg.Scsi.Device.MainChannelSelection.UserData,
                            Bwg.Scsi.Device.SubChannelMode.None,
                            Bwg.Scsi.Device.C2ErrorMode.Mode294,
                            1, false, lba, (uint)count, scratch, 30);
                        string sense = st == Bwg.Scsi.Device.CommandStatus.Success
                            ? ""
                            : $" sense={dev.GetSenseKey()}/{dev.GetSenseAsc():X2}/{dev.GetSenseAscq():X2}";
                        Console.WriteLine($"  rel={rel} count={count,2}: {st}{sense}");
                    }
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(scratch);
            }
        }
        finally
        {
            dev.Close();
        }
        return 0;
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
