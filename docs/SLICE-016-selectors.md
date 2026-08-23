# CUETools Linux Slice Brief: SLICE-016 analog selectors

Version: 0.1. Date: 2026-08-23. Status: Awaiting owner decision.
Companion documents: ARCHITECTURE.md, DECISION-LOG.md (D-080 the
analog control scope, D-082 where the shared skills live),
SLICE-014-analog-controls.md, SLICE-015-soft-body-keys.md.

Designed 2026-08-23 after the owner asked what the analog equivalent
of a combo box is, and asked for the good options to be rendered
rather than argued. Both controls below were built as real Avalonia
controls and captured through the Skia headless harness in both
themes. Nothing here gets built until "Approved for build" is stamped.

---

## 1. What the slice proves

- **Capability added:** the last stock Fluent control on the analog
  pages stops looking borrowed. A selector either shows all its
  positions as a bank of interlocked keys, or sits behind a machined
  window with a thumbwheel.
- **The claim being tested:** a dropdown hides its options behind a
  click, which is the right trade for a list and the wrong trade for
  three fixed choices that fit on one line. Eleven of the app's
  fifteen selector sites are not lists.
- **What it costs:** less than it looks. Each key in a bank is the
  machined key SLICE-014 already shipped, wired into a selection
  group. The window is a template restyle of `ComboBox`, so the
  interaction, keyboard handling, and screen reader behaviour are
  untouched.

## 2. The two controls

**A - the interlocked bank.** Every option visible and in its own
order, one key held down. The physical reference is a tape transport
or a radio preset row, where the keys are mechanically linked so a new
one going down forces the old one up. Each cell carries the crown,
shoulder, and lip from SLICE-014 and the lamp pip from the lit-switch
family; the housing is the recessed well the switches already use.

An ordered set reads as a ladder and an unordered set reads as radio
keys, with no change to the widget. Two positions is a rocker, which
is the same control at n=2 rather than a third control.

**B - the machined window.** A recessed glass window showing the
current value with its position beneath it, and a ridged thumbwheel on
the right edge. The wheel is the part that says there is more than
one. Click still opens the list.

## 3. The split rule, and what it selects

Bank when the option set is fixed, short, and up to about five. Window
when the list is open, the labels are long, or the count varies.
Option names and counts below were read from source, not memory:
`RipViewModel.OutputLayouts`, `SettingsViewModel.RipOutputLayouts`,
`SettingsViewModel.CtdbSharingChoices`, the inline `ComboBoxItem` set
in `RipView.axaml`, and the Advanced page tooltips.

| Selector | Page | Options | Control |
|---|---|---|---|
| quality | Rip | Burst, Secure, Paranoid, Salvage | bank |
| layout | Rip, Settings | Tracks, Image + embedded CUE | bank (rocker) |
| Metadata search | Advanced | None, Fast, Default, Extensive | bank |
| Album art search | Advanced | None, Primary, Extensive | bank |
| Network | Advanced | None, System, Custom | bank |
| CTDB sharing | Settings | Ask before sharing, Always share, Never share | window |
| Drive | Rip, Drive | whatever is attached | window |
| release | Rip | 0 to N lookup results | window |
| Preset | Naming | user defined | window |
| Encoder, mode, enum | Encoder settings | up to 9 codecs, per-codec modes | window |
| Action | Queue | short fixed set | window |

CTDB sharing is the interesting one. It has only three options, which
says bank, but its labels run about 330 pixels laid out as one, which
says window. Long labels win. Shortening them to Ask, Always, Never
would make it a clean bank, but that is a wording change to a consent
control and it is the owner's call.

Queue action is the other judgement call. It sits inside a table row,
where a bank fights the column sizing.

## 4. Width

The four-key quality bank measures about 300 pixels against roughly
140 for the dropdown it replaces. The Rip page has that room at its
1200 pixel default and the Advanced rows have it at 440, so nothing
here forces a change to the reflow breakpoints recorded for the Rip
page. This must be re-measured, not assumed, if a bank ever lands on
the Rip page's right rail.

## 5. Out of scope, on purpose

- **The rotary knob.** Judged and rejected before rendering. It is
  hostile to a mouse, hostile to a screen reader, and spends more
  space than a bank for less information.
- **Replacing every dropdown.** Four sites stay windowed because they
  are genuinely lists. The slice is not "remove ComboBox".
- **Rewording any option.** See CTDB sharing above.
- **The position readout.** `2 / 7` under a window value is a tape
  counter. It earns its space on release, where the count is a real
  signal, and may be noise on a fixed three-item list. Decide per
  site, not globally.

## 6. Evidence so far

Rendered 2026-08-23 through the Skia headless harness, both themes, at
true size, including the resting, hovered, and held states of a bank
and the open state of a window. The sheets carry the same three
Advanced rows in the proposed and current form for direct comparison.
Published for owner review; the harness patch was reverted after each
capture and the working tree verified clean.

## 7. Acceptance criteria

Not written. This document exists to carry the design past a context
boundary and to be decided against, not to be built from yet. When
approved, the criteria come from SLICE-014's, plus one addition: a
bank must be operable by keyboard with arrow keys moving the held
position, and each key must expose its own accessible name and
selected state.
