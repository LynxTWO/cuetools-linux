# CUETools Linux Slice Brief: SLICE-001 Verify

Version: 0.2. Date: 2026-08-11. Status: Approved for build.
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md.

One narrow, production-quality section. This document is the agent's build
boundary: if it is not in here, it does not get built.

## 1. What the slice proves

- **Central claim:** the CUETools 2026 experience can live natively on
  Linux: real engine, real verdicts, the real visual identity, in a small
  package. If verify works honestly and beautifully on Ubuntu, the rest of
  the port is execution, not hope.
- **The slice in one line:** a user opens CUETools Linux, picks an album
  folder, and gets an honest AccurateRip + CTDB verdict with a named report,
  online or offline.
- **Honest stakes:** verification is CUETools' most-used job. If real Linux
  users run this slice and it fails to deliver value, the project deserves a
  rethink.

## 2. The walkthrough

1. User installs the .deb (or runs the AppImage) on clean Ubuntu LTS and
   launches CUETools Linux. The app opens to the Verify page in the 2026
   dark theme, under 2 seconds to interactive.
2. User picks an album folder (or cue sheet). The engine parses the cue and
   decodes WAV/FLAC/ALAC audio with per-track progress.
3. The app computes checksums and queries AccurateRip and CTDB. Online: the
   per-track verdict table and confidence render, and a dated named report
   is written next to the album. Offline: the verdict shows "not verified,"
   a backfill journal entry is written, and the report says so honestly.
4. User toggles dark/light live, opens the report, and (if the run was
   offline) later reconnects: verification backfill replays automatically
   and appends dated verification evidence without altering the original
   report.

Every step runs against the real architecture: real engine submodule, real
shared app core, real journal, real packaging.

## 3. In scope, with build order

| Item | Notes |
| --- | --- |
| Fork-side shared app core extraction, verify-scoped | The governed CUETools.Wpf refactor (D-012), limited to what Verify needs; WPF behavior preserved with evidence, via fork PR |
| Avalonia shell: window, nav rail, Verify page | 2026 theme identity, dark/light runtime toggle (D-023, D-030) |
| Engine on Linux via submodule | Pinned commit, pwsh vendor staging proven (S-1) |
| Verify transaction + report | AccurateRip + CTDB lookups, honest degradation |
| Backfill journal + verification lane | EDD section 5 entity, automatic replay |
| Packaging: .deb + AppImage + size gate + clean-install CI test | D-007, D-025 |

Build order (spikes close before dependents):

| Milestone | Contents |
| --- | --- |
| M1 | SPIKE S-1: fork engine libs build and test under pwsh on Ubuntu (closes Q-003/U-005). SPIKE S-2: DeviceId probe on Linux (closes Q-002/U-006). SPIKE S-3: Avalonia 12.1 NativeAOT publish, size and startup measured (verifies A-002) |
| M2 | Fork PR: shared app core extraction (verify-scoped), WPF regression evidence |
| M3 | Linux repo scaffold: submodule, src layout, CI gates (build, tests, format, lint) |
| M4 | Verify page, theme port, engine integration; Avalonia.Headless tests |
| M5 | Journal seam + verification backfill + offline tests |
| M6 | Packaging, size gate, clean-install test, walkthrough evidence |

Build-order status (2026-08-11): M1 done (SPIKES-2026-08-11.md). M2 done:
fork PR LynxTWO/cuetools_2026#8 merged with all four Windows CI runs green
(warning-gated build, 241-test modern lane, fuzz, classic devenv solution).
M3 done: PR #2 merged, Linux CI green over the pinned submodule. M4 in
progress: 2026 palette, fonts, and shell structure render in both themes
(docs/evidence/2026-08-11-theme-dark.png, -light.png); themed NativeAOT
publish measures 32 MB installed, 7.8 MB gzipped binary, within D-025.
M4 continued (same day): the Verify workspace runs end to end on Ubuntu
against the shared app core (fork PR #9 async seams merged): cue parsed,
WAV decoded, per-track CRC32s computed, live AccurateRip and CTDB queried,
honest verdict rendered (docs/evidence/2026-08-11-verify-fixture-dark.png).
Compiled-in managed codec registration (Flake, ALAC) per the Linux
no-plugin-scanning posture. Headless UI test added (xunit v3 +
Avalonia.Headless). Light-theme verify evidence:
docs/evidence/2026-08-11-verify-fixture-light.png. Real-disc walkthrough ran 2026-08-12: the owner's pressed CD
(19 tracks, 54:31), ripped with stock GStreamer cdparanoia, verified
"accurate | confidence 4" against AccurateRip natively on Linux
(docs/evidence/2026-08-12-verify-real-disc-dark.png). The same run
upgraded O-001 to an open bug: CTDB lookups fail on Linux (XML parse
error), so the CTDB half of R-001 and the metadata naming are pending
that fix. Remaining in M4: the O-001 CTDB fix and the accessibility
pass.

## 4. Out of scope, on purpose

| Excluded | Where it will connect later | Log entry |
| --- | --- | --- |
| Repair | Verify page's repair action (next slice candidate) | D-032 |
| Rip (drive access module) | ICDRipper seam, ADD section 5 | D-032 |
| Convert | Job orchestration | D-032 |
| Enrichment backfill | Journal's enrichment lane (ADD section 10) | D-011 |
| Native codec runtime (.so builds) | Codec registration seam | D-032 |
| Flatpak, translations, CLI, Wayland default | ADD section 10 seams | D-007, D-030 |

## 5. Stubs and their debts

| Stub | Real version arrives when | Behavior for now | Log entry |
| --- | --- | --- | --- |
| Sources needing native codecs (WavPack, APE, TTA) | Codec runtime slice | Honest "codec unavailable in this build" row; never a crash or silent skip | D-032 |
| Nav rail shows only Verify and Settings (minimal) | Pages arrive with their slices | Absent, not grayed-out fakes | D-032 |
| Enrichment lane in journal | Enrichment slice | Lane field exists in the format; entries never created | D-011 |

## 6. Modules touched

Desktop shell (new), Shared app core (fork, extracted), Job orchestration
(verify transaction + journal), Engine (consumed), Platform services (XDG
paths, dialogs). Not touched: Drive access, Codec runtime (beyond managed
codecs). Interfaces exercised: processor job API, shared-core surface,
journal format.

## 7. Data subset

BackfillJournalEntry in full production shape (EDD section 5): all fields,
formatVersion, deletion rule, migration posture. Settings (theme choice,
last folder). DriveProfile: not created in this slice.

## 8. Acceptance criteria

| ID | Criterion | Verified by |
| --- | --- | --- |
| S-001 | Given a known-good WAV/FLAC/ALAC fixture album, when verified online, then per-track AR + CTDB results display and a dated report file exists | CI smoke test (R-001) |
| S-002 | Given no network, when verified, then "not verified" displays, a journal entry exists, and the report states the degradation | Integration test (R-005) |
| S-003 | Given a pending journal entry and network back, when the app runs, then backfill appends dated verification evidence and the original report is byte-unchanged | Integration test (R-005) |
| S-004 | Given a WavPack source, when verify is attempted, then the honest unavailable row appears; no crash | Unit/UI test |
| S-005 | The .deb and AppImage install and run the walkthrough on clean Ubuntu LTS containers | Packaging CI job (R-010) |
| S-006 | Download <= 30 MB per package; installed <= 60 MB; cold start <= 2 s | Size gate + startup timer (R-006) |
| S-007 | Dark/light toggles live; side-by-side against WPF passes owner review | Manual pass (R-007) |
| S-008 | Verify page fully keyboard-operable; controls exposed via AT-SPI2 (Orca smoke) | Manual pass (R-008) |

## 9. Verification evidence required

- [ ] Automated tests for S-001 to S-006 passing in CI.
- [ ] Walkthrough executed on a clean environment, result recorded.
- [ ] Error paths exercised: providers down (ADD section 13 row 1); codec
      unavailable (row 3).
- [ ] EDD section 17 per-change checklist satisfied for every change.

## 10. Agent guardrails for this build

- **Boundary:** only section 6 modules, only section 7 data.
- **Stop and ask before:** everything on the D-028 stop list.
- **Mode separation:** discovery, then implementation, then verification.
- **Conflicts:** if reality contradicts these documents, stop and surface it.

## 11. Slice definition of done

- [ ] All acceptance criteria pass with linked evidence.
- [ ] All EDD guardrails hold. No unlabeled shortcuts inside the boundary.
- [ ] Documents updated: statuses, unknowns, log entries for lessons.
- [ ] Human walkthrough completed and approved by Daniel Boyd.

## 12. What this unlocks

- SLICE-002 candidate: Repair (CTDB parity repair inside the Verify page,
  the fork's preservation-transaction rules carried).
- SLICE-002 alternative: Codec runtime (.so builds + trust manifest),
  unlocking WavPack/APE verification and preparing Convert.

---

*Approved for build by: Daniel Boyd, 2026-08-11. When section 11 closes with
evidence, mark the status Done and update ADD section 15 before opening the
next brief.*
