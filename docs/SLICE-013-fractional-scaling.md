# CUETools Linux Slice Brief: SLICE-013 fractional scaling

Version: 0.2. Date: 2026-08-20. Status: Approved for build.
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md
(D-074, D-075, D-076).

Designed in the 2026-08-20 owner interview (Conductor protocol; intake
plus two sections, decisions D-074 to D-076). Until "Approved for
build" is stamped below, nothing here gets built.

---

## 1. What the slice proves

- **Capability added:** the interface lays out elegantly under the
  operating system's fractional scaling at 100, 125, 150, 175, and 200
  percent: pages reflow as logical space shrinks, nothing clips, and
  the work a page exists for stays reachable at every factor.
- **The slice in one line:** a user at 200 percent on a 1080p screen
  runs a secure Test & Copy end to end, with the rail as a lit icon
  strip, the track grid and evidence chips fully visible, and never a
  clipped control.
- **Honest stakes:** today the window's minimum (960x560 logical) is
  taller than the 960x540 desktop a 200 percent 1080p screen offers,
  so the app cannot even fit; at 175 percent it fits with nothing to
  spare and two-column pages have no adaptation at all.

## 2. The walkthrough

1. The user sets Ubuntu to 150 percent scaling. The app opens at its
   default size; every page renders the full layout, physically larger
   and pixel-sharp.
2. They raise scaling to 175 percent. The window no longer has 1140
   logical pixels of width: the rail collapses to the icon strip (one
   backlit key per page, active page lit, tooltips carrying the full
   names), and Settings' two columns stack to one. Nothing clips.
3. At 200 percent (960x540 logical desktop) the window fits, because
   the minimum has dropped to 640x480. The Rip page keeps its right
   OUTPUT/RUN rail and its track grid; the instrument visuals sit at
   their declared legibility floors; content beyond the height scrolls
   vertically.
4. They shrink the window below 860 logical pixels by hand: the floor
   applies - horizontal scrolling appears instead of clipping, exactly
   as the existing Rip-page rule promises.
5. Back at 100 percent, everything returns to today's full layout.
   Nothing about a running rip changed at any step: reflow is display
   only.

## 3. In scope, with build order

| Item | Notes |
|---|---|
| Icon set for the strip: ten rail entries (Verify & Repair, Convert, Queue, Rip, Settings, Report, Naming, Drive & Read, Advanced, How a CD Works), etched-backlit-key style, both themes, legible at strip size | D-076: designed FIRST, in the analog language the analog controls pass will inherit; render-verified at strip size before any reflow work consumes it |
| Rail collapse: full rail at >= 1140 logical width, icon strip below (D-075); tooltips carry full names; active-page key lit | The strip is the one element on every page; its behavior sets the design's tone |
| Two-column pages stack to one below 1140 (Settings, Naming; same mechanism for any future two-column page) | Bounded proportional layout, no new scroll axes above the floor |
| Rip and Verify tier-one treatment: right rail retained at compact, track grid flexes, Test/Copy CRC evidence and primary actions reachable at every matrix point | Extends the existing 1200 px operability rule to the compact and floor states |
| Instrument legibility floors declared per control (disc read map, track strip, pass lane, VU pair, scope traces); pages reflow around an instrument at its floor rather than shrinking it further | D-074: proportional with floors; no simplified variants |
| Floor state below 860 logical: horizontal scroll instead of clipping; window minimum drops to 640x480 | Makes the 200-percent-on-1080p desktop a supported host |
| Automated render matrix harness: tier-one pages at 5 factors x 2 themes x 3 layout states, assertion-backed no-clipping checks | The regression net for every later layout change |

## 4. Out of scope, on purpose

| Excluded | Where it will connect later | Log entry |
|---|---|---|
| The Windows head | Ports the design with its own evidence once the Linux head proves it | D-074 |
| Compact low-resolution mode | Deferred until a real small-screen machine exists; written trigger in the log | D-074 |
| Analog restyle of buttons, checkboxes, and other controls | Its own slice, inheriting the icon language this slice creates | D-076 |
| Simplified small-mode instrument variants | Rejected; floors instead | D-074 |
| A user-facing rail-collapse toggle | Collapse is automatic by width; a manual override is a new setting nobody asked for | D-076 |

## 5. Stubs and their debts

None planned. If an icon proves illegible at strip size during
design, that entry may temporarily carry a two-letter monogram, named
here as a debt with the icon's redesign as its retirement - not
silently shipped.

## 6. Modules touched

CUETools.Linux.App only: MainWindow (rail states, breakpoint
observation), Views (Settings, Naming stacking; Rip and Verify
tier-one treatments), Controls (legibility floors on the instrument
controls), Theme (icon strip styles, icon geometry), tests (the render
matrix harness and its assertions). No engine changes, no App.Core
changes expected; if one becomes necessary it stops the work and gets
surfaced first.

## 7. Data subset

No persistent data. Rail state derives from window width at render
time; nothing new is written to settings, history, or output
contracts.

## 8. Acceptance criteria

| ID | Criterion | Verified by |
|---|---|---|
| S13-001 | At every matrix point (5 factors x 2 themes x 3 states, Rip and Verify), no control clips and no page scrolls horizontally above the floor | Automated render matrix, assertion-backed |
| S13-002 | The rail collapses to the icon strip exactly at the 1140 threshold and returns at the same threshold; active page stays selected across the transition | Matrix renders both sides of the threshold + unit test |
| S13-003 | Every strip icon is identifiable at strip size in both themes; tooltips carry the full page names | Owner eyeball of the icon sheet render, both themes |
| S13-004 | The window fits and operates on a 960x540 logical desktop (200 percent on 1080p): minimum 640x480, Rip page primary actions, drive selection, and Test/Copy CRC evidence reachable | Live GNOME walkthrough + matrix floor renders |
| S13-005 | Each instrument stops shrinking at its declared floor and the page reflows around it; the floors are written in the brief's build notes with their numbers | Matrix renders + per-control unit tests |
| S13-006 | A rip running through a breakpoint transition is unaffected: reflow is display only | Unit test on the reflow path + live spot check |
| S13-007 | One live GNOME fractional-scaling session by the owner at all five factors on real hardware, walking Rip and Verify | Owner walkthrough, recorded in this brief |

## 9. Verification evidence required

- [x] Icon sheet render (all ten, both themes, strip size and 2x) eyeballed
  and approved by the owner before reflow work begins. Approved
  2026-08-21 ("looks great, lets do it all") after one markup round:
  the lit glyphs gained real light falloff (zero-offset shadow blur of
  the stroke, the key halo's own physics) in place of a hard-edged
  band. Sheets: docs/evidence/2026-08-20-slice013-icon-sheet-*.png.
- [x] The automated matrix passing in CI (ScaleMatrixTests: the real graph
  and real MainWindow through all five factors plus floor and minimum,
  both themes; no-clipping, held-floor, reachability, and
  selection-survival assertions).
- [x] Floor-state renders at 640x480 and 960x540 archived for both themes:
  docs/evidence/slice013-matrix/ (14 captures, the full matrix).
- [ ] The owner's live walkthrough at 100/125/150/175/200 on GNOME,
  with anything that reads wrong logged as findings before Done.
- [x] Honesty audit: the floor state HOLDS the 860-wide layout, so no
  instrument ever lays out below its 860 size (the matrix asserts the
  disc read map's width at the floor); the CRC evidence column is
  asserted reachable at every matrix point.

## 10. Agent guardrails for this build

- **Boundary:** only the modules in section 6; display-only, no engine
  seams, no output-contract changes, no new persisted settings.
- **Stop and ask before:** any App.Core change, any new dependency,
  anything that would move or hide Test/Copy CRC evidence, and any
  tension between reflow and an honesty rule (evidence visibility wins
  until the owner rules).
- **Mode separation:** icons first (approval gate), then reflow
  mechanics with the matrix harness, then tier-one page treatments,
  then live evidence.
- **Conflicts:** if a breakpoint number produces a broken intermediate
  state on a real page, stop and surface it with renders; do not
  invent a third breakpoint silently (D-076 chose two).

## 11. Slice definition of done

- [ ] All acceptance criteria pass with linked evidence.
- [ ] No unlabeled shortcuts inside the boundary.
- [ ] Documents updated: statuses, log entries for anything learned,
  manual pages for anything user-visible.
- [ ] Human walkthrough completed and approved by the owner.

## 12. What this unlocks

- The analog controls pass (next slice candidate): lamp checkboxes,
  machined keys, backlit legends - inheriting this slice's icon
  language.
- The Windows head's scaling port, with this slice's matrix as its
  target behavior.
- Reopening compact low-resolution mode the day its trigger fires,
  on top of proven reflow mechanics.

---

*Approved for build by the owner, 2026-08-20 (D-078), design as
written. Sequencing note: the D-077 house-voice writing pass lands
before this slice's build begins; within the slice, the icon set is
the first build item and section 9's first checkbox is the first
gate.*
