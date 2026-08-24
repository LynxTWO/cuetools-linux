# CUETools Linux Slice Brief: SLICE-016 analog selectors

Version: 0.2. Date: 2026-08-23. Status: Approved for build (owner stamp
2026-08-23, "I agree with your mockups of the dropdowns"), and built.
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

CTDB sharing is the interesting one, and it stays a window. Measured
2026-08-23: the Settings row is 455 DIP wide and its control gets 296.
A three-key bank with these labels needs about 352 for the keys, and
the row label "Share verified rips with CTDB" takes another 190, so it
does not fit beside its label at any padding worth having. It WOULD
fit with the label stacked above, but that is the only row on the page
that would break the label-left rhythm.
The alternative is shortening the options to Ask, Always, Never, which
reads fine under that row label and would make a clean bank. Not
taken. It is a wording change to a consent control, `settings.md` line
34 quotes the current strings, and the long labels are the ones that
still say what they mean when a screen reader reads the option without
the row label. If the owner wants the bank, the change is those three
strings, the manual line, and an accessible name on the bank; it is
about five minutes and it is reversible.

Queue action BECAME a bank. The earlier reasoning was wrong on a fact:
it is not in a table row. It sits in a `LastChildFill="False"` toolbar
where a wider control just pushes the next one along, and its options
are `Verify` and `Convert` - two short labels, which is the rocker
case. Both now show without a click.

## 4. Width, and where v0.1 got it wrong

v0.1 said the Rip page "has room at its 1200 pixel default". That was
the page width, not the width the control actually gets, and the first
render of the build cut "Salvage" off the quality bank. The Rip page's
right rail is **300 DIP**, about 274 of it usable, and a four-key bank
beside its own label does not fit there at any padding worth having.
The measurement that mattered was never taken.

Three things came out of that, and all three are in the build:

- The label goes **above** the bank in the rail rather than beside it,
  which returns roughly 58 DIP.
- A `compact` bank tightens the keys to 8 DIP of horizontal padding at
  11.5 point. All four quality positions then fit on one line in 274.
- The bank's items panel is a **WrapPanel**, so when a bank genuinely
  cannot fit, it becomes a two-row keypad instead of losing a position.
  Console hardware solves the same problem the same way.

The Advanced rows are 440 DIP and take a bank beside its label with
room to spare, which is what the mockups showed.

A layout test now pins this. The scale matrix could not see it: it
asserts the PAGE does not overflow its viewport, and a bank inside a
fixed-width column overflows the column while the page stays exactly
as wide as before. The first clipped render was green on every other
assertion in the suite.

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

## 7. What the build did

Both controls are styles rather than new control classes, which is why
this landed as one pass.

- The **bank** is `ListBox` with `Classes="bank"`. Arrow-key selection,
  the accessible name and selected state per key, and the
  `ItemsSource`/`SelectedItem`/`SelectedIndex` trio the old
  ComboBoxes were already bound to all come with `ListBox`, so a call
  site changes one element name and keeps its bindings.
- The **window** is a `ComboBox` template restyle with
  `Classes="window"`, so opening, keyboard handling and the accessible
  role are untouched. `Classes="window counted"` adds the position
  readout, which stays opt-in per the v0.1 ruling.

Sixteen call sites converted: SEVEN to banks (quality, layout on Rip
and on Settings, metadata search, album art search, network, and the
Queue action added 2026-08-23) and nine to windows.

## 8. Verification

- 189 tests green, including two new layout tests: no bank position is
  ever cut off at any of the five supported widths in either theme,
  and every position is visible, non-empty, and reachable.
- Rendered through the Skia harness at 1200 DIP in both themes: the
  Rip page rail and the Advanced page rows.
- The existing scale matrix still passes unchanged, which matters
  because banks are wider than the dropdowns they replaced.

Still owed: the owner's eyes on the running build, and a decision on
the two open calls in section 3 (CTDB sharing wording, Queue action).
