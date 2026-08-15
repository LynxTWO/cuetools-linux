# CUETools Linux Slice Brief: SLICE-010 rip progress visuals

Version: 0.1 Draft. Date: 2026-08-14. Status: Proposed.
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md.

Design approved by the owner on 2026-08-14 (Conductor mini-interview,
decisions D-057 to D-059). Build is queued: SLICE-009 (rip) is the active
slice, and at selection time this brief competes with the settings page
for next. Until "Approved for build" is stamped below, nothing here gets
built.

---

## 1. What the slice proves

- **Capability added:** during a rip or Test & Copy, the user can see at a
  glance where the run is (per track), which phase it is in (Test or
  Copy), and how hard the mode is working (pass and vote activity), on
  surfaces that follow the same honesty rules as the existing visuals.
- **The slice in one line:** a user starts a Paranoid Test & Copy and can
  watch each track fill hollow during Test, solid during Copy, with pass
  ticks landing during re-reads and amber ticks naming the tracks that
  hurt.
- **Honest stakes:** without this, the only progress signal is one
  whole-job bar; the user cannot tell Test from Copy, cannot see which
  track is in flight, and mode choice changes nothing visually. Neither
  the Linux head nor the WPF head has ever had per-track progress.

## 2. The walkthrough

1. The user starts a secure Test & Copy on a loaded disc.
2. The track grid rows fill left to right as the head crosses each
   track; the active track carries an accent edge. The new track strip
   above the job bar mirrors the same progress, one segment per track,
   segment width proportional to track duration.
3. The job bar shows a TEST chip and hollow fill; when Copy starts, the
   chip flips to COPY, the fill turns solid, and each track's solid Copy
   fill draws inside its retained hollow Test outline.
4. During re-reads, the pass lane under the job bar shows literal pass
   ticks as votes land; a track whose sectors needed re-reads gains a
   thin amber edge tick on its strip segment with the literal counts in
   the tooltip.
5. The run ends: tracks show clean completion, or red-edged
   completion-with-unrecoverable where Salvage kept going, matching the
   log's failed-sector counts exactly.

Every step runs against the real engine progress events. No simulated
progress, no synthetic events.

## 3. In scope, with build order

| Item | Notes |
|---|---|
| Shared track-progress state in RipViewModel (App.Core, fork PR): per-track fraction, phase, mode, pass activity, per-track reread and unrecoverable counts, derived from ReadProgressArgs + TOC boundaries | Pure derivation over existing engine events; unit-testable without hardware |
| Track grid row fill + active-track accent (Avalonia) | Grid rows stay pure progress; no damage marks in the grid |
| Segmented track strip control (Avalonia) | One segment per track, duration-proportional, 1200 px default width must stay legible for a 24-track disc |
| Job bar enrichment: phase chip, mode chip, pass lane (Avalonia) | Pass lane is window-scoped and lives only on the job bar; Burst shows no lane (absence is the honest display of one-pass mode) |
| Phase persistence: hollow Test state retained under solid Copy fill | Visual mirror of the immutable phase-evidence rule |
| Damage ticks on strip segments + unrecoverable-only terminal marking | Literal counts in tooltips; routine corrections do not mark |

Build order: VM state + tests first (fork PR), then the two controls,
then job-bar enrichment, then live evidence runs.

## 4. Out of scope, on purpose

| Excluded | Where it will connect later | Log entry |
|---|---|---|
| WPF head controls (parity) | Port the two controls; RipViewModel already carries the state | D-059 |
| Per-track pass display | Pass structure is window-scoped; revisit only if evidence shows demand | D-058 |
| Error heat-mapping on the strip | DiscReadMap owns sector-level damage display | D-058 |
| Correction-level (non-unrecoverable) terminal marking | Badges and logs already grade quality | D-058 |

## 5. Stubs and their debts

None. Inside the boundary everything is real; the slice adds no stubs.

## 6. Modules touched

CUETools.App.Core (RipViewModel + tests, fork repo), CUETools.Linux.App
Controls/RipVisuals.cs or a sibling control file, Views/RipView.axaml(.cs).
The VM state must flow through the existing progress event seams
(IUiDispatcher marshaling, RipTelemetryMailbox where applicable); controls
consume VM state only, never engine objects.

## 7. Data subset

No persistent data. All state is per-run and in-memory; nothing new is
written to settings, history, or output contracts.

## 8. Acceptance criteria

| ID | Criterion | Verified by |
|---|---|---|
| S10-001 | During a real secure T&C, track rows and strip segments fill in step with the engine's reported position; the active track is visually distinct | Live run, screenshots (local only) |
| S10-002 | Test phase renders hollow with TEST chip; Copy renders solid with COPY chip; a test-completed track keeps its hollow outline under the Copy fill | Live run, screenshots (local only) |
| S10-003 | Pass lane shows literal pass ticks during re-reads; Burst mode shows no pass lane | Live secure + burst runs |
| S10-004 | On the damaged reference disc, amber ticks appear on exactly the tracks whose logged reread counts are nonzero, and tooltips carry the literal counts | Live damaged-disc run cross-checked against the log |
| S10-005 | A track completed with unrecoverable sectors renders the red-edged terminal state; its tooltip count equals the log's failed-sector count | Salvage-path run on the damaged disc |
| S10-006 | VM track-progress derivation is covered by unit tests: fraction math from TOC boundaries, phase transitions, per-track counter attribution, Image + Embedded Cue layout | Suite, no hardware |
| S10-007 | The Rip page stays operable at 1200 px: strip legible for a 24-track disc, no horizontal clipping | Live check at default width |

## 9. Verification evidence required

- [ ] Suite additions for S10-006 passing in CI.
- [ ] Live evidence runs for S10-001..005 and S10-007 recorded in the
  brief (screenshots stay local per the scrub rule; the brief records
  counts and outcomes only).
- [ ] Honesty audit: every displayed number traced to a literal engine
  value; smoothing confined to animation.

## 10. Agent guardrails for this build

- **Boundary:** only the modules in section 6; no engine changes, no new
  events, no output-contract changes.
- **Stop and ask before:** any new dependency, any engine seam change,
  anything that would alter rip behavior rather than display it.
- **Mode separation:** VM + tests, then controls, then live evidence.
- **Conflicts:** if the engine's events prove insufficient for honest
  per-track attribution, stop and surface it; do not approximate
  silently.

## 11. Slice definition of done

- [ ] All acceptance criteria pass with linked evidence.
- [ ] No unlabeled shortcuts inside the boundary.
- [ ] Documents updated: statuses, log entries for anything learned.
- [ ] Human walkthrough completed and approved by the owner.

## 12. What this unlocks

- WPF parity slice: port the two controls; the shared VM already speaks
  the language (D-059's extension point).
- Settings page slice: the other current candidate for "next"; unrelated
  surface, competes at selection time.

---

*Approved for build by: pending (design approved by owner 2026-08-14).
Until then, this brief is a proposal. When section 11 closes with
evidence, mark the status Done and update ADD section 15 before opening
the next brief.*
