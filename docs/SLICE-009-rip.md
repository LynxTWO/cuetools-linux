# CUETools Linux Slice Brief: SLICE-009 Rip

Version: 0.1 Draft. Date: 2026-08-13. Status: Approved for build (the
owner's rip mini-interview, D-053..D-056, answered every design
question; built under the standing autonomy grant with hardware
sessions owner-scheduled).
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md.

## 1. What the slice adds, and the shipping rule

- **Capability:** secure CD ripping on Linux - the complete assurance
  stack: drive calibration, cache defeat, the flagged vote and retry
  policy, Test & Copy with phase evidence (D-055), and the full RipView
  UI (D-054).
- **The rule (D-046):** full-secure-or-nothing. NOTHING rips in a
  shipped build until the complete invariant set works on Linux. During
  development only, a --rip-diagnostic capability compiled out of
  release builds entirely gathers drive evidence (D-053).

## 2. The architecture finding that shapes everything

The fork's assurance logic lives in CDDriveReader (CUETools.Ripper.SCSI,
2,909 lines: calibration, cache defeat, C2 handling, the retry/vote
policies, the sense-identity rules the project invariants specify). It
sits on Bwg.Scsi.Device (3,769 lines), whose Windows transport funnels
through exactly TWO DeviceIoControl call sites inside one SendCommand
gate.

So the Linux port is a TRANSPORT port, not a ripper port: an SG_IO
implementation (Linux sg_io_hdr ioctl on /dev/sg*) behind the same
Device API, leaving every line of invariant code unchanged and shared
with Windows. Reimplementing the ripper would forfeit the battle-tested
invariants; splitting the transport preserves them by construction.

Device naming: ICDRipper.Open takes a Windows drive letter; Linux maps
letters to the sorted /dev/sr* set with the true device path recorded
in evidence (design detail for increment 1).

## 3. The drive matrix (D-045/D-056)

- Matrix drive 1: the laptop's PLDS DVD-RW DU8A5SH, firmware BU51
  (/dev/sr0, /dev/sg1; cdrom group access verified in place).
- Matrix drive 2: the desktop's HL-DT-ST BD-RE WH16NS40 firmware 1.05 -
  the drive the fork's 3E/02 and 08/0A carve-outs were written on -
  joining EARLY during calibration design (owner connects it and says
  when).
- Matrix update, 2026-08-13: the owner connected two external USB
  drives with discs loaded, so the matrix is now THREE drives on one
  host: the PLDS internal (/dev/sr0), an ASUS BW-16D1HT 3.11
  (/dev/sr1), and the WH16NS40 1.05 itself (/dev/sr2). D-056's early
  involvement is satisfied without the desktop hop.

## 4. Increments

1. **SG_IO transport**: Linux Device implementation behind the two-site
   funnel; INQUIRY, TOC read, and drive identity proven live against
   the PLDS drive via --rip-diagnostic.
2. **Read engine bring-up**: READ CD (BEh) payloads, C2 pointers, drive
   offset detection, read-command probing - evidence-gathered per drive.
3. **Calibration + cache defeat**: the D8-B-era invariants (capability
   probe, proven flush size, complete-or-explicit eviction) live on
   Linux; both matrix drives exercised.
4. **Secure rip + Test & Copy**: the full two-pass flow with held
   state, phase evidence, and tie-breaks (D-055).
5. **RipView parity** (D-054): the complete page, incrementally, with
   telemetry feeding the existing CodecScope.
6. **Process-per-drive**: the multi-drive window model when a second
   simultaneous drive exists to prove it.

## 5. Acceptance criteria (headline rows; each increment adds detail)

| ID | Criterion | Verified by |
| --- | --- | --- |
| S9-001 | The transport passes the fork's SCSI command layer unchanged over SG_IO | Increment 1 evidence |
| S9-002 | Calibration, cache defeat, and the retry/vote invariants run identically on Linux | Increment 3 + matrix evidence |
| S9-003 | A real disc rips secure with Test & Copy evidence and database verification | Increment 4 walkthrough |
| S9-004 | RipView parity | Increment 5 + owner walkthrough |
| S9-005 | No shipped build exposes any read path before S9-001..S9-004 all hold | Release gating (D-046) |

## 6. Increment 1 receipt (2026-08-13)

**The SG_IO transport is live: all three matrix drives answered INQUIRY
and READ TOC through the fork's unchanged command layer on the first
run.** Receipt: fork PR #23 (feat(ripper): Linux SG_IO transport behind
the WinDev seam) plus the --rip-diagnostic run below, exit 0.

    rip-diagnostic: 3 drive(s) enumerated
      A /dev/sg1 identity=[PLDS     - DVD-RW DU8A5SH] tracks=24 audioSectors=307668
      B /dev/sg2 identity=[ASUS     - BW-16D1HT] tracks=12 audioSectors=203523
      C /dev/sg3 identity=[HL-DT-ST - BD-RE  WH16NS40] tracks=7 audioSectors=180087

Engineer detail, measured on this host:

- The whole Windows transport is WinDev (the 190-line CreateFile handle
  base) plus the SendCommand32/64 funnel in Device. The Linux side sits
  behind that exact seam: LinuxSg.cs (sg_io_hdr interop), platform
  splits in WinDev Open/Close/Control, and Device.SendCommandLinux
  mirroring the SendCommand64 contract including the R112
  identity-clearing rules and the actual-transfer write-back (from the
  sg residual). No assurance code above the transport changed.
- Letter mapping refinement over section 2's sorted-set sketch: letters
  bind 1:1 to kernel sr numbers (A = /dev/sr0), a pure function both
  CDDrivesList and LinuxSg derive independently; the sg node is
  resolved via /sys/block/srN/device/scsi_generic and the true device
  path is printed in evidence.
- The dev flag is compiled out of Release per D-053, verified at byte
  level: strings -el finds "rip-diagnostic" twice in the Debug assembly
  and zero times in Release, and no ripper assemblies reach the Release
  output.
- Test suite after the change: 49/49 passed.

S9-001 is **verified** for INQUIRY and TOC; payload reads (READ CD) are
increment 2.

## 7. Increment 2 receipt (2026-08-13)

**Real READ CD payload transfers work on all three drives: the fork's
read-command matrix probe ran unchanged over SG_IO and every drive
chose the BEh command with C2 pointers on its first candidate.**
Receipt: the extended --rip-diagnostic run below, exit 0.

    A /dev/sg1 PLDS DU8A5SH      read-command: BEh, 12h, 42h, 16 blocks at a time  ar-offset: +6 samples
    B /dev/sg2 ASUS BW-16D1HT    read-command: BEh, 12h, 42h, 16 blocks at a time  ar-offset: +6 samples
    C /dev/sg3 HL-DT-ST WH16NS40 read-command: BEh, 12h, 42h, 16 blocks at a time  ar-offset: +6 samples

Engineer detail: the probe is TestReadCommand in CDDriveReader - the
BEh/D8h x C2-mode x main-channel matrix with the three-region damage
sweep - so a Success verdict means multi-sector audio payloads plus C2
data crossed the transport and passed the engine's own checks. Probe
times: 2.3-2.9 s cold, 16-38 ms once the drive spun up. The 16-block
window is the engine's own NSECTORS design maximum, not a transport
limit. Drive read offsets resolve through the same HTTPS-fetched
AccurateRip DriveOffsets.bin the WPF head uses; all three drives are
known +6-sample models.

**The secure multi-pass read engine itself also ran end to end on all
three drives: one full PrefetchSector window each (2400 sectors,
1,411,200 samples, ~32 s of audio) at the default correction quality,
2 matching passes, 0 failed sectors, 0 communication retries.**
Receipt: the same --rip-diagnostic run, secure-read lines, exit 0.

    A PLDS DU8A5SH      secure-read: 1411200 samples ( 8401 ms window), passes=2, events=300, failedSectors=0, commRetries=0
    B ASUS BW-16D1HT    secure-read: 1411200 samples ( 9587 ms window), passes=2, events=300, failedSectors=0, commRetries=0
    C HL-DT-ST WH16NS40 secure-read: 1411200 samples (12116 ms window), passes=2, events=300, failedSectors=0, commRetries=0

cacheDefeatBytes reads 0 on every drive - truthful per R113: no
calibration transaction has established cache defeat yet. That is the
increment 3 boundary, exactly where the brief drew it: the vote engine,
window management, and per-sector accounting are proven; what remains
is the calibration prerequisite chain (capability refresh, proven flush
size, complete-or-explicit eviction) and its WPF service extraction.

## 8. Increment 3 receipt (2026-08-13)

**The complete drive calibration transaction ran on all three drives:
every drive demonstrated caching, the cache-defeat search confirmed a
proven flush size on each, and the records persisted.** Receipt: fork
PR #24 (the App.Core rip extraction) plus the --rip-diagnostic
calibration lines below, exit 0.

    A PLDS DU8A5SH      cacheDefeat=Flush:786432 (Confirmed)  offset=+6  overread in=False out=False  speed 44-4234 kbps
    B ASUS BW-16D1HT    cacheDefeat=Flush:786432 (Confirmed)  offset=+6  overread in=True  out=False  speed 44-8467 kbps
    C HL-DT-ST WH16NS40 cacheDefeat=Flush:786432 (Confirmed)  offset=+6  overread in=True  out=False  speed 44-8467 kbps

Engineer detail: caching was demonstrated honestly per drive (first
read of the probe region 108-204 ms, immediate re-read 1-5 ms), and the
eviction search settled the same 786,432-byte proven flush size on all
three. Overread capability differs per drive and is recorded, not
assumed. Persistence initially failed with the known PublishAot
landmine (reflection System.Text.Json disabled even in JIT builds);
GzJson now resolves through a source-generated StoreJsonContext - the
same cure as RepairEvidence.ToJson - and the calibration record
(version 2026.2.0) saves and reloads.

## 9. Increment 4 receipt, first pass (2026-08-13)

**The complete secure Test & Copy transaction ran end to end on Linux
and committed a published output set.** Receipt: fork PR #25 plus the
--rip-tc run below (drive B, the ASUS BW-16D1HT), exit 0.

    verdict[0:Test] AR 29/71, CTDB 105/107, accurate=True
    verdict[1:Copy] AR 29/71, CTDB 105/107, accurate=True
    rip-tc result: ok=True outcome=Passed readsUsed=2
    output: 12 file(s) committed
    history: recorded=True

Engineer detail, measured:

- Two independent cache-defeated secure reads, ~264 s and ~275 s, with
  the drive's Confirmed 786,432-byte flush forced before every secure
  re-read. Matching checksums on all 12 tracks; both reads verified by
  both databases; slip analysis found the reads aligned; zero failed
  windows, zero communication retries, 1785 extended-timeout reads
  absorbed by the engine's timeout policy.
- The committed set carries the full output contract: 12 track FLACs,
  sanitized artist/album-stem cue, rip log, AccurateRip report and TOC,
  the machine-readable rip.verify, and .cuetools-complete written last
  (18 items). Identity strings stay out of this document per the
  scrubbed-logging rule.
- Three Linux blockers surfaced and fixed at their intended seams (fork
  PR #25): keep-awake degrades honestly, tray control speaks the CDROM
  ioctls through the same gated funnel, and the last reflection-STJ
  site (VerifyHistoryStore.ToJson, in the commit path) moved to the
  source-generated context. The first failing commit ALSO proved the
  held-state contract live: both verified staged reads were kept and
  reported.

Remaining for increment 4: the tie-break third read on a disagreeing
disc (needs damaged media), Held-state user surface, and CTDB repair
evidence flow on Test & Copy outputs - the user-facing halves land
with RipView parity (increment 5).

## 10. Increment 5 receipt, first pass (2026-08-13)

**The Rip page runs on Linux: the full RipViewModel composes over the
SG_IO stack and the insert-and-it-reads flow worked on first launch.**
Receipt: fork PR #26 (seams + move) plus this repo's rip-view change;
live evidence on this host with three real drives.

- Fork PR #26 moved RipViewModel (2,290 lines), AlbumArtService, the
  artwork models, AppStatusService, and AppLaunchOptions to App.Core
  behind five new platform seams (UI timers, artwork previews, display
  capabilities, image transcoding, plus the existing dispatcher/prompt/
  file-dialog seams). The WPF head keeps identical behavior through its
  own implementations; 512 Wpf.Tests stayed green.
- This head supplies Avalonia implementations: DispatcherTimer-backed
  timers, decode-bounded Bitmap previews, a SkiaSharp transcoder with
  the same passthrough-within-cap JPEG contract, and an honest
  no-hardware-3D capability answer (the 2D read map renders).
- Live: launched with --rip-page, drive A auto-selected with its
  INQUIRY identity, the inserted disc read automatically (24 tracks,
  68:22), five release matches arrived with the MusicBrainz release
  selected, cover art fetched and displayed, and the run rail armed
  (Rip / Test & Copy / Verify only). Suite 49/49. The evidence
  screenshot stays local: it names the owner's disc, and this repo
  stays scrubbed.

Remaining for full D-054 parity: the live visuals (read map, VU,
speed graph, re-read zoom), artwork browser, salvage/output-layout
controls, recent-rips panel, and the held-state walkthrough evidence.

## 11. Hardware finding and eviction fix (2026-08-13 night)

**Driving the app's own secure verify surfaced a real firmware quirk,
and the invariants caught it exactly as designed.** The internal PLDS
DU8A5SH aborted the cache-defeat eviction's final partial chunk
(ABORTED COMMAND 0B/00/00) twice at the identical sector, and the
engine failed closed both times with the full scrubbed diagnostic
identity. A dev read-shape probe (--rip-probe, D-053) characterized
the cause in minutes: this drive deterministically aborts BEh+C2
reads of exactly 15 or 2 sectors at any location, while both USB
drives accept every probed count. Receipt: the fork's
docs/review/2026-08-13-plds-partial-chunk-abort.md.

The fix (fork PR #29) pads the eviction plan to whole chunks - at
least the required bytes, never a partial-count tail, no sense
classification touched. Proof: the same verify that failed twice ran
to completion, 3,265 s including a long outer-rim deep-recovery grind
(passes into the 30s at the floor speed, slip 0, errors converging to
0), with zero cache-defeat retries across hundreds of padded flushes.
The live visuals rendered throughout ("Verifying... 93%", disc map
near the rim, controls locked, Stop armed).

Open observation, honestly recorded: the completed verify reported AR
0/82 and CTDB 0/235 - the disc ID is known to both databases but this
read matched no pressing. Drive B's Test & Copy proved the stack
byte-exact against both databases, so this is either a different
mastering of this disc or a drive-A fidelity question. The
discriminating experiment is an owner action: swap this disc into the
ASUS or WH16NS40 and verify again. The remaining payload-tail batch
question is the fork's D10 decision.

Closure (2026-08-14, after the owner's disc swap and USB replug):

- Corrected 2026-08-14 evening: the mystery disc is **the damaged
  SLICE-002 reference disc**, not a pressing variant. Its database
  identity (AR total 82, CTDB total 235) matches the scratched disc
  exactly, and the owner's live Paranoid Test & Copy of it surfaced the
  status text the earlier monitoring had truncated: CTDB "differs in
  112 samples, confidence 207" - repairable damage, honestly detected.
  The zero-exact-match verdicts on every drive are the damage, read
  consistently. The three-drive byte-exact matrix below is unaffected:
  that evidence used the clean 12-track disc.
- **All three drives are byte-exact through the full Linux stack**: the
  PLDS, ASUS, and WH16NS40 each verified the same database-anchored
  disc to the identical verdict (AR 29/71 accurate, CTDB 105/107).
- The WH16NS40's 24/00-on-everything state was initially blamed on
  processes killed mid-command; the owner's live walkthrough corrected
  that: the ASUS entered the identical state organically during the
  Paranoid Test & Copy of the damaged disc (Test complete with honest
  give-ups; Copy failed closed at the eviction wall 20 minutes in).
  Both incidents are USB-bridged drives under extended recovery
  grinding on the damaged disc; the SATA internal drive survived a full
  deep-recovery pass of the same disc. The pattern, receipts, and the
  open recovery-strategy question live in the fork's
  docs/review/2026-08-14-usb-wedge-finding.md and D11. D10 was resolved
  drive-scoped (fork PR #30) before this closure.
- Dev tools added along the way: --rip-verify-cli <letter> (headless
  RipService.RunVerify, the deterministic path for per-drive
  experiments) and --rip-seq-probe <letter> (sequenced reads for
  state-dependent rejection hunting).

## 12. Increment 6 receipt (2026-08-14)

**Process-per-drive works on Linux, and proving it surfaced a real
cross-process safety hole.** The drive lease's exclusion relied on
Windows file-sharing violations, which .NET does not enforce between
processes on Unix: a second process acquired a held drive and read it
concurrently, observed live. The fork fix takes an explicit advisory
lock on the lease file on Unix (Windows unchanged) and adds the Linux
physical identity (the block device's MAJ:MIN), so both the letter and
the mechanism collide before any hardware contact.

Receipts on this host:

- With a headless verify holding drive A, a second claim fails
  instantly: "Drive A: is already in use by another CUETools job" -
  before touching the hardware. Before the fix the second process
  reached "Verifying..." on the same drive.
- A --secondary-drive-window --drive B process launches titled for its
  drive, lands on the Rip page with B pre-selected (published on the
  drive service before the view model builds, the WPF pattern, so no
  transient read of the first drive), reads B's disc, and shows the
  verify-history x2 CRC evidence - all while A's job continues in the
  other process. Each process writes its own collision-safe log.
- Secondary windows never publish shared settings: the save-on-exit
  hook is gated on the window role.

## 13. Increment 3 extraction inventory (executed 2026-08-13, fork PR #24)

The calibration and rip services have no WPF API coupling - the plan is
the proven M2 pattern (move to CUETools.App.Core, namespaces stay
CUETools.Wpf.*). Scouted set:

- CUETools.Wpf/Accuracy: AdaptiveSpeedController (59), CacheDefeatSearch
  (43), DriveCalibration (97), DriveCalibrationService (228),
  ReadOffsetProbe (98), TestAndCopyLog (120), TestAndCopyResolver (115).
- CUETools.Wpf/Services: IDriveService (55), DriveService (509),
  RipService incl. IRipService (2936), LevelMeteringRipper (107).
- CUETools.Wpf/Models: DiscInfo, DriveDetails, ReleaseMatch, TrackItem.

IDiagnosticLog already lives in App.Core. App.Core gains project
references to CUETools.Ripper.SCSI and Bwg.Scsi - possible now because
increment 1 gave both a neutral net8.0 TargetFramework. RipViewModel
(2,290 lines, 27 WPF/Dispatcher touches) stays put: that is the D-054
RipView parity surface, increment 5, behind the usual dispatcher seams.

## 14. Damaged-disc session receipts (2026-08-14/15 night)

The deliberate damaged-disc session on the ASUS (USB) banked two
evidence rows and produced the complete D11 wedge characterization
(fork `docs/review/2026-08-14-usb-wedge-finding.md`, decisions
D-060..D-062, SLICE-011 brief).

**Stuck-drive message, live (Paranoid Test & Copy, ~24 min in).** The
run reproduced the wedge during eviction grinding; the engine failed
closed and the drive bar carried the new guidance verbatim. The wedge
was then probed while stuck (canned IllegalRequest/24/00 for every
media command, kernel silent) and cured only by an enclosure power
cycle; the guidance now leads with the power cycle (fork PR #38, pin
PR #53).

**StopOnUnrecoverable, live (verify, Stop on, 723 s).** Log receipt
from the per-window diagnostic log, 2026-08-15 00:15:

```
00:15:01.187  rip.recovery  window=259200 pass=63 running=3 fresh=3/2400 speed=0x slip=0
00:15:01.188  rip.reread    gave up on window at 85% unresolvedSectors=3 (unreadable by drive)
00:15:01.190  rip           stop requested
00:15:01.194  rip           stopped by user after 723s
```

Sixty-three passes over the damaged window at the recovery floor, the
engine's give-up classification (3 sectors unreadable by drive, window
abandoned at 85%), and the stop issued 2 ms after classification,
never before it: the "applied only after the configured evidence and
retry policy has classified a sector as unrecoverable" invariant,
observed end to end. The drive released cleanly; the kernel logged
nothing. (The "stopped by user" label is the shared stop path; the
timestamps establish what pulled the trigger.)

**Still unbanked:** the tie-break third read and the Held-state UX.
Both need a damaged-disc Test & Copy completing both reads with a
mismatch, which means Stop off and surviving roughly 50 minutes of
grinding on hardware that has wedged at ~24 minutes twice. Next
session's call: these rows either ride with SLICE-009 sign-off or
transfer to the SLICE-011 live-evidence session, where a wedge
mid-attempt is not a failure but the dialog's own test case.

---

*Interview answered by: Daniel Boyd, 2026-08-13 (D-053..D-056).*
