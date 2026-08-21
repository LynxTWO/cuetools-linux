# CUETools Linux Slice Brief: SLICE-015 soft-body rubber keys

Version: 0.1. Date: 2026-08-21. Status: Awaiting approval.
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md
(D-080 scope, D-081 the glyph recipe, D-082 the open skill question).

Designed 2026-08-21 from an 11-agent research fan-out with compiled
probes on this workstation's real GPU, three adversarial reviews, and
a follow-up measurement that overturned the package's own
recommendation. Until "Approved for build" is stamped below, nothing
here gets built.

---

## 1. What the slice proves

- **Capability added:** a button stops being a rectangle that changes
  color and becomes a soft rubber cap over a center plunger. Press it
  and the rubber collapses under your finger, tilts about the plunger,
  dimples locally where you pressed, and springs back.
- **The slice in one line:** a user clicks the top-left corner of Test
  & Copy and that corner visibly sinks deepest while the cap rocks
  about its middle, the legend shears with the rubber, and the whole
  thing rebounds with a rubber overshoot.
- **Honest stakes:** the current press is a 1.2 px rigid slide. Every
  key in the app moves identically no matter where it is clicked, and
  the WPF head's earlier attempt at an off-center press was
  deliberately dialed back (fork commit 982eb921) because a pure shear
  "looked like a tilting card". That failure is diagnosed: a shear is
  a rigid tilt with no squish. This slice models the squish.

## 2. The walkthrough

1. The user moves the pointer over Rip. The key acknowledges without
   deforming (D-080: press-only geometry in this build).
2. They press it dead center. The cap travels straight down on its
   plunger, the face shades into the depression, the legend compresses.
3. They press Test & Copy near its top-left corner instead. That
   corner sinks deepest, the cap rocks about its center, the opposite
   corner lifts slightly, and the dimple sits under the pointer.
4. They release. The cap rebounds with a soft overshoot and settles.
5. They press, drag off the key, and release. The click does not fire,
   and the release is DEAD: no rubbery rebound, because nothing
   happened (see section 3, cancelled-press honesty).
6. They tab to a key with the keyboard and press Space. It presses
   dead center with no tilt, because a keyboard has no press point.
7. Everything above holds at 100 and 150 percent display scaling, and
   the label stays as crisp as a natively drawn one.

## 3. In scope, with build order

| Item | Notes |
|---|---|
| The pure deformation model: a public function of (press point, sample point, press amount, aspect) with named gain constants | Four terms: plunger travel, tilt about the center fulcrum, a local dimple, and a skirt clamp pinning the rim (D-080). Testable with nothing rendered |
| Press-state and input lane: press point capture, keyboard center-press, drag-off cancel, press-to-disabled transition | The repo's first pointer-input tests |
| The mesh renderer: ICustomDrawOperation + SKCanvas.DrawVertices, face and label textured per D-081's exact recipe | Rest is drawn natively; the mesh exists only while deforming |
| Spring release: damped rebound on a landed click, dead release on a cancelled one | One boolean, and it turns decoration into signal |
| Focus indicator replacement: a two-tone rim | The current single-token ring measures 1.00:1 on the light-theme accent key (invisible); this fixes a real existing defect |
| Applied to all 61 Buttons plus the 10 RailStripKey navigation keys | D-080 |
| The soft-body skill, cross-head | See section 6 and D-082 |

Build order is deliberate and front-loads risk:

0. Write the silent-failure traps down first (section 10).
1. The pure math plus its tests. No renderer.
2. The press/input lane plus its tests.
3. A flag-gated build in front of the owner at 1.0x AND 1.5x, both
   themes, with a corner press held. **This is a gate, not a phase.**
   The question asked out loud: does the shaded dip read as below the
   surface, or does the outline need to move?
4. The mesh renderer and the glyph recipe.
5. Evidence, then the skill.

## 4. Out of scope, on purpose

| Excluded | Where it connects later | Log entry |
|---|---|---|
| Hover deformation | Revisit after the press physics ship and the mid-rip frame budget is measured | D-080 |
| An un-pinned rim (outline crossing below neighbours) | Owner escalation after seeing the shaded dip | D-080 |
| Deforming disabled keys | Dead keys stay dead-solid; the press-to-disabled transition IS in scope | D-080 |
| The WPF head | Ports after the Linux head proves the look, replacing BendyButton.cs | D-074 pattern |
| ComboBox, NumericUpDown, TextBox | Over-theming data entry is a real risk; decide with evidence later | D-077 |

## 5. Stubs and their debts

None planned. The rest-native/press-mesh split means the flat path is
not a fallback stub but the resting appearance of every key, exercised
continuously by every user on every idle frame.

## 6. Modules touched

CUETools.Linux.App only: Theme/AnalogControls.axaml, a new
Controls/SoftBodyKey.cs (compare RipVisuals.cs at 563 lines), tests.
No engine, no App.Core, no behavior changes, no layout-contract
changes.

Plus knowledge capture, which D-082 blocks: a new sibling skill
(soft-body or deformable controls) alongside lit-panel-controls rather
than folded into it. They share one philosophy sentence and nothing
else: one models light transport and is state-driven, the other models
an elastic solid and is pointer-driven; their techniques, their
verification disciplines and their characteristic failures are all
different. lit-panel-controls also needs three repairs first: a
duplicated render-verify section, a frontmatter that still claims
"WPF/XAML specific" under an Avalonia section, and cited harness paths
that do not exist.

## 7. Data subset

No persistent data.

## 8. Acceptance criteria

| ID | Criterion | Verified by |
|---|---|---|
| S15-001 | A corner press produces greater local displacement at that corner than a center press does, and lifts the opposite corner, within budget | Pure model unit tests |
| S15-002 | The rim stays pinned: perimeter displacement is zero and no geometry escapes the layout rect | Pure model unit tests |
| S15-003 | Every press point on every key size stays inside the depth budget at all five scale-matrix factors | Model tests at the matrix factors |
| S15-004 | Keyboard Space/Enter presses dead center with zero tilt; press-then-drag-off cancels the click and releases dead | Headless pointer/key tests |
| S15-005 | A resting key is byte-identical to a natively drawn key at every scaling | Render test, local Skia lane |
| S15-006 | Label ink and sharpness under press match a native draw within the D-081 tolerances at 1.0x through 2.0x | Render test, local Skia lane |
| S15-007 | The scale matrix and every contrast contract still pass | CI |
| S15-008 | No frame drops on the Rip page mid-rip while a key animates | Live measurement, owner present |
| S15-009 | The owner presses corners on real hardware and calls it right | Live eyeball |

## 9. Verification evidence required

- [ ] Model tests green in the committed harness (no renderer needed).
- [ ] Pointer/keyboard input tests green.
- [ ] Rest-identity and press-fidelity renders archived, both themes,
  at 1.0x / 1.25x / 1.5x / 2.0x.
- [ ] The LCD-subpixel check on the REAL Avalonia window surface
  (D-081's named build gate), not on an SKBitmap-backed canvas.
- [ ] Software-rendering and mid-rip frame-budget measurements.
- [ ] Owner walkthrough.

## 10. Agent guardrails for this build

- **Boundary:** display only; the modules in section 6.
- **Stop and ask before:** un-pinning the rim, adding hover geometry,
  any App.Core change, anything that moves or hides Test/Copy CRC
  evidence.
- **Three silent failures to record BEFORE writing code**, each
  measured, each of which a future maintainer would otherwise
  "fix" back: (a) any TransformOperationsTransition on RenderTransform
  flattens a perspective matrix to its affine part with no error and
  no test failure - the app's own transition and Fluent's both have to
  go; (b) a code-set RenderTransform outranks the :pressed style
  trigger, so the existing 1.2 px depression stops firing while the
  XAML still reads as though it works; (c) if the face ever becomes
  self-drawing, every future `Button.x /template/ Border#keyFace`
  selector parses clean, loads clean, matches the element and paints
  nothing.
- **Never touch an Avalonia Visual from inside the draw operation.**
  It runs on the render thread in the real app and on the UI thread in
  every test this repo can run, so the hazard is structurally
  invisible to our harness. Ship state by message; assert the thread
  inside the operation so a future edit is caught at the seam.

## 11. Slice definition of done

- [ ] All acceptance criteria pass with linked evidence.
- [ ] No unlabeled shortcuts inside the boundary.
- [ ] Documents updated: statuses, ADD section 15, the manual if any
  user-visible text describes the old press.
- [ ] The soft-body skill written, and D-082 resolved so it is
  actually discoverable from both heads.
- [ ] Owner walkthrough complete.

## 12. Kill criteria

Abandon or scope down if any of these is observed mid-build:

- The owner sees the shaded dip at 1.0x and 1.5x and cannot tell the
  outline is not bending. Ship the cheap transform path and drop the
  mesh entirely.
- Un-pinning the rim proves necessary AND the resulting dip pushes a
  30 px key's clickable area under the WCAG 24x24 minimum, or overlaps
  a neighbour at the 1200 px Rip-page width.
- A rest frame is not byte-identical to the native draw once the
  D-081 recipe is applied. The rest-native swap is what makes app-wide
  scope affordable; if it flashes, drop one or the other, never ship
  both.
- A render-thread state race survives the message-passing shape.
- The Rip page drops frames mid-rip while a key animates. The app's
  core job outranks any amount of rubber.

---

*Awaiting approval.*
