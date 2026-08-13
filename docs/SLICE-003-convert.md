# CUETools Linux Slice Brief: SLICE-003 Convert

Version: 0.1 Draft. Date: 2026-08-12. Status: Approved for build (owner
selected 2026-08-12, D-040; built under the owner's overnight autonomy
grant with owner-facing rows queued for morning).
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md,
SLICE-001-verify.md, SLICE-002-repair.md.

## 1. What the slice adds, and why it is next

- **Capability:** the classic CUETools conversion path as its own page: a
  cue sheet, album folder, or file with an embedded cue goes in; every
  track is re-encoded to the chosen output format with cue fidelity kept.
  The page carries the codec picker (healthy implementations selectable,
  unavailable rows explanatory and unselectable), the encoder settings
  surface, and the CodecScope/ConvertScope educational visualizations.
- **Why now:** owner selected it (D-040). It is the highest-value surface
  that needs no native codec runtime: the compiled-in managed encoders
  (Flake FLAC, ALAC, WAV) already cover the main lossless targets. It also
  builds the encoder catalog and settings seam that the codec-runtime
  slice later plugs native implementations into.

## 2. The walkthrough

1. User picks a source (cue, folder, or embedded-cue file) and an output
   folder. Before a source is chosen, CodecScope explains what the target
   codec does; with a source chosen, ConvertScope shows the round trip
   (source -> PCM -> target) live.
2. User picks the output format and, optionally, a specific codec
   implementation via the picker; "tune" opens the encoder settings with
   every option the codec exposes, each with an explanation.
3. Convert runs with live progress; the result state reports what was
   written and where.
4. Output keeps the album's cue fidelity: the converted set carries a cue
   sheet describing the same disc layout, and verifying the output against
   the databases reaches the same verdict as the source.

## 3. In scope

| Item | Notes |
| --- | --- |
| Fork extraction of the convert closure | ConvertService, EncoderCatalog, CodecCatalogModels, ConvertViewModel, EncoderSettingsViewModel move to CUETools.App.Core (M2 pattern: namespaces stay CUETools.Wpf.*, dispatcher use goes through the IUiDispatcher seam) |
| Convert page in the Linux app | Port of ConvertView in the 2026 identity |
| Codec picker | Healthy rows selectable; unavailable rows explanatory and unselectable (codec-readiness rule); an extension alone is not implementation identity |
| Encoder settings surface | Port of EncoderSettingsViewModel's window |
| CodecScope + ConvertScope | Ported per the disc-read-visualization portable pattern (RepairScope precedent); may degrade to a simpler presentation with a logged note if it balloons |
| Headless tests | Scripted IConvertService paths: success, failure, busy-state gating, codec selection rules |
| Real conversion evidence | An owner-album conversion on Linux, output verified via the Verify page |

## 4. Out of scope, on purpose

| Excluded | Where it connects later | Log entry |
| --- | --- | --- |
| Rip | Later slice | D-032 |
| Native codec runtime (WavPack/APE/native FLAC) | Codec-runtime slice plugs into this slice's catalog seam | D-040 |
| External command encoders (lame, etc.) | Curated-encoder manifest work, Windows-parity review needed for Linux | D-040 |
| Queue page | Batch conversion UX; single-conversion page first | D-040 |
| Enrichment backfill | Journal enrichment lane | D-011 |

## 5. Stubs and their debts

None new planned. SLICE-001's stubs stand.

## 6. Modules touched

Fork shared app core (convert closure moves in; WPF head keeps working
unchanged), Desktop shell (Convert page, picker, settings, scopes),
Composition (IConvertService wiring beside IVerifyService). The journal is
untouched: conversion is local work and needs no database, so there is no
offline lane to journal.

## 7. Data subset

No new app-owned entities. Conversion consumes the engine's CUESheet
processing and the config's output-path templates.

## 8. Acceptance criteria

| ID | Criterion | Verified by |
| --- | --- | --- |
| S3-001 | A cue-sheet album converts to the selected managed format with every track re-encoded and a cue sheet describing the same disc layout | Evidence run + output inspection |
| S3-002 | The converted output verifies against AccurateRip/CTDB with the same disc identity as the source | Verify page run on the output |
| S3-003 | Only formats with a working encoder in this build are offered; unavailable codec rows are explanatory and unselectable | Headless tests |
| S3-004 | Encoder settings persist per codec and are honored by the conversion | Headless test + evidence run |
| S3-005 | A failed conversion reports plainly and leaves no partial output masquerading as complete | Headless test |
| S3-006 | Real-album walkthrough on Linux: owner-provided album converted and output verified | Owner-assisted evidence run (queued for morning) |

## 9. Verification evidence required

- [x] Headless tests for S3-003/S3-004/S3-005 passing in CI (five
      ConvertFlowTests, including a real fixture conversion through the
      production ConvertService; suite 28/28).
- [x] A real conversion evidence run for S3-001/S3-002 (2026-08-13): the
      repaired album from SLICE-002's walkthrough (24 FLAC tracks)
      converted to Apple Lossless on Linux, published atomically with
      `.cuetools-complete`, and the converted set verifies **AccurateRip
      accurate confidence 29 / CTDB 207** - the identical verdict to its
      source, proving the round trip bit-exact by the databases
      themselves. Screenshots:
      docs/evidence/2026-08-13-convert-alac-complete.png,
      docs/evidence/2026-08-13-convert-alac-verified.png. Bonus finding:
      the first ALAC encode on Linux exposed a kernel32 P/Invoke in
      ALACWriter's thread-time statistic (fork PR #16 guards it; ALAC is
      the only codec that defines INTEROP).
- [ ] EDD section 17 per-change checklist per change.
- [x] CodecScope/ConvertScope visualization port (2026-08-13): both
      scopes render on Linux with the shared CodecMath/LossyMath doing the
      real predictor and masking work. Evidence: CodecScope idle showing
      the FLAC pipeline at 5.1 bits/sample ~32% of PCM
      (docs/evidence/2026-08-13-codecscope-idle.png), ConvertScope live
      during an ALAC -> FLAC round trip with real decoded audio flowing
      and both sides at 9.0 bits/sample
      (docs/evidence/2026-08-13-convertscope-live.png). The rip-telemetry
      sample feed for CodecScope arrives with the rip slice.

## 10. Agent guardrails for this build

Boundary per section 6; D-028 stop list as relaxed by D-033/D-040
autonomy; mode separation; conflicts stop and surface. Owner rows
(S3-006 sign-off, S-007/S-008) queue for morning.

## 11. Slice definition of done

All acceptance criteria evidenced; documents updated; owner walkthrough
approval. **Met 2026-08-13**: owner walked the app and approved
(morning walkthrough covering slices 3-6). SLICE-003 is Done.

## 12. What this unlocks

- Codec-runtime slice: native implementations join the catalog this slice
  builds (WavPack/APE verify, repair, and convert; closes A-003).
- Queue/batch conversion UX.
- Rip slice output side: a rip is a conversion whose source is a drive.

---

*Approved for build by: Daniel Boyd, 2026-08-12 (D-040 slice selection).*
