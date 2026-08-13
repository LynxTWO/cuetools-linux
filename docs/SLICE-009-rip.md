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

---

*Interview answered by: Daniel Boyd, 2026-08-13 (D-053..D-056).*
