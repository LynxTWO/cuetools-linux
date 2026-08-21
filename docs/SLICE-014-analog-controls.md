# CUETools Linux Slice Brief: SLICE-014 analog controls

Version: 0.1. Date: 2026-08-21. Status: Awaiting approval.
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md
(D-075/D-076 icon language, D-077 house voice).

The owner asked for this pass on 2026-08-20 ("there's gotta be a way to
really bring everything into 2027 analog") and approved the concept
board's direction the same day. This brief turns the board into a
boundary. Until "Approved for build" is stamped below, nothing here
gets built.

---

## 1. What the slice proves

- **Capability added:** every button and checkbox in the app speaks the
  bench language the switches, strip keys, and lamps already speak: lit
  controls with real light physics instead of stock toolkit widgets.
- **The slice in one line:** a user glances at the Rip page and every
  control reads as part of one instrument - lamp checkboxes glowing
  through etched ticks, machined keys that depress when pressed, and
  transport keys whose backlit legends go dark when a key is dead.
- **Honest stakes:** the stock blue checkboxes and flat gray buttons
  are the last toolkit-default controls on the bench; disabled today
  reads as gray-on-gray text instead of an unpowered control.

## 2. The walkthrough

1. The user opens the Rip page. The **cue** and **log** checkboxes are
   small lamp buttons: recessed square housings whose lens glows teal
   through a dark etched tick when checked, dead housing with a ghost
   tick when not.
2. Every button is a machined key: top-lit face, drop shadow, and a
   physical depression (a shift down, a tightened shadow) while
   pressed.
3. The RUN group (**Rip**, **Test & Copy**, **Verify only**, **Stop**)
   reads as a transport row: each key carries a backlit legend strip -
   accent-lit when the key is armed, dim for secondary actions, and
   dark (with dark text) when the key is disabled, the way a dead
   backlight reads on real gear.
4. The consent and recovery dialogs' buttons wear the same key face;
   the Share button's accent rule (D-070 era) is unchanged.
5. Both themes hold: the light theme's keys are pale machined plastic
   with the deep accent for lit legends, per the approved icon-sheet
   physics.

## 3. In scope, with build order

| Item | Notes |
|---|---|
| Lamp checkbox: a `CheckBox.lit`-equivalent control theme (housing, lens glow, etched tick, halo), one-knob Lamp ramp recolor, warm/cool transitions per the lit-panel model | The Rip page's cue/log first; any future checkbox inherits |
| Machined key: the default Button control theme restyled (gradient face, edge, drop shadow, pressed depression, hover sheen, focus ring) | App-wide by theme; no per-view edits |
| Transport keys: a `Button.transport` class adding the backlit legend strip (accent-lit armed, dim secondary, dead when disabled) | The Rip page's RUN group; Read disc/Eject stay plain keys |
| Disabled-as-unpowered: disabled keys go dark-legend instead of gray-text | The honesty read: unavailable = unpowered |
| Render zoo evidence: all states x both themes, 2x and 1x, archived | Same loop that tuned the switches and strip keys |

Build order: lamp checkbox (smallest, proves the recolor), then the
key theme, then transport, then evidence.

## 4. Out of scope, on purpose

| Excluded | Where it will connect later | Log entry |
|---|---|---|
| ComboBox, NumericUpDown, TextBox restyles | A later pass if the owner wants the full sweep; risk of over-theming data-entry controls | D-077's match-the-stakes rule |
| WPF head parity for these controls | Rides the Windows port lane once the Linux head proves the look | D-074 pattern |
| The theme toggle button's special casing | It is already a plain button and stays one | - |
| New colors or ramps | The five D-076 ramps are the palette | D-076 |

## 5. Stubs and their debts

None planned.

## 6. Modules touched

CUETools.Linux.App only: Theme (LampCheck.axaml, MachinedKey.axaml or
additions to existing theme files), Views/RipView.axaml (cue/log
class swap, RUN group transport class), tests (state contracts,
contrast pins for new text-on-key pairs). No engine, no App.Core, no
behavior changes.

## 7. Data subset

No persistent data.

## 8. Acceptance criteria

| ID | Criterion | Verified by |
|---|---|---|
| S14-001 | Checked lamp checkboxes glow with the falloff physics (no hard-edged band); unchecked read as dead housings; the etched tick is visible in both states | Render zoo, both themes, 2x and 1x |
| S14-002 | Every button in the app renders as a machined key with a visible pressed depression; keyboard focus stays visible | Render zoo + existing focus-ring conventions |
| S14-003 | Transport keys' legends are accent-lit when armed, dark when disabled; disabled text is the dead-legend treatment, not stock gray | Render zoo + a disabled-state render of the real RUN group |
| S14-004 | The scale matrix still passes: the restyle changes no layout contract | ScaleMatrixTests in CI |
| S14-005 | Text on every key state meets WCAG AA in both themes | ContrastContractTests extended to the key faces |
| S14-006 | The owner walks the app and calls the look done | Live eyeball |

## 9. Verification evidence required

- [ ] Render zoo archived (docs/evidence/), all states, both themes.
- [ ] Suite green including the extended contrast pins.
- [ ] Owner's live eyeball on the real app.

## 10. Agent guardrails for this build

- **Boundary:** display only; the modules in section 6; no behavior,
  layout-contract, or consent-flow changes.
- **Stop and ask before:** any new ramp or color, any change to the
  consent dialog's accent rules, anything that would make a control's
  STATE less legible than today (the unpowered read must never cost
  the user the ability to see a control exists).
- **Mode separation:** checkbox, then key theme, then transport, then
  evidence; render-verify at each step.

## 11. Slice definition of done

- [ ] All acceptance criteria pass with linked evidence.
- [ ] No unlabeled shortcuts inside the boundary.
- [ ] Documents updated: statuses, ADD section 15, manual touches if
  any text describes the old controls.
- [ ] Owner eyeball complete.

## 12. What this unlocks

- The WPF head's analog pass, from a proven Linux look.
- The full-sweep restyle decision (combos, spinners) with real
  evidence about whether the bench language helps or hurts data entry.

---

*Awaiting approval.*
