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

## 10. Increment 3 extraction inventory (executed 2026-08-13, fork PR #24)

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

---

*Interview answered by: Daniel Boyd, 2026-08-13 (D-053..D-056).*
