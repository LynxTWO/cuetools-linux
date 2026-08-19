# CUETools Linux Slice Brief: SLICE-011 guided drive recovery

Version: 0.2. Date: 2026-08-14. Status: Approved for build
(2026-08-15, D-064). Also carries the two evidence rows transferred
from SLICE-009 (D-063): S11-007 and S11-008 below.
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md,
and the fork's `docs/review/2026-08-14-usb-wedge-finding.md` (the
evidence this slice is built on).

Design approved by the owner on 2026-08-14 in conversation: the owner
proposed a rung-by-rung recovery dialog with per-drive memory; the AI
refined it against the live wedge characterization (no software rungs;
app-verified physical rungs) and the owner accepted the refinements.
Decisions D-060 to D-062. Build is queued: SLICE-009 is active and
SLICE-010 is also Proposed; selection order is the owner's call.

---

## 1. What the slice proves

- **Capability added:** when a drive enters the characterized stuck
  state, the user is walked through the physical recovery that actually
  works, each step verified live by the app, and the drive's recovery
  history makes the next incident faster.
- **The slice in one line:** the stuck-drive failure opens a guided
  dialog: "unplug the USB cable and replug it" (with a visual), the app
  watches the drive come back and probes it, says "still stuck - now
  unplug its power," confirms the cure, and offers a fresh retry.
- **Honest stakes:** without this, the user gets the (correct, shipped)
  fatal message and is on their own; with it, the one cure that works
  is reached in order, without guesswork, and repeat incidents skip
  straight to what cured this drive before.

## 2. The walkthrough

1. A secure operation fails with the stuck-drive signature (the
   shipped `IsUnresponsiveDriveSignature` classification).
2. The failure surface offers "Recover the drive..."; the dialog opens
   showing the drive's identity and, if incident history exists, what
   cured it last time.
3. Rung A (skipped when history says it never works for this drive):
   "Unplug the drive's USB cable, wait two seconds, plug it back in,"
   with a simple visual. The app watches for re-enumeration and probes
   the TOC unprivileged; on failure it says so and advances.
4. Rung B: "Unplug the drive's power cord (or power it off), wait two
   seconds, restore power," with a visual. Same live verification.
5. On cure: the incident record is written (trigger context, rungs
   attempted, curing rung), and the dialog offers "Retry now" - a
   fresh operation through the normal calibrated paths. The failed
   transaction stays failed.
6. If Rung B fails: honest terminal state - try a different port or
   cable, or the drive may need service - and the incident is recorded
   as uncured.

## 3. In scope, with build order

| Item | Notes |
| --- | --- |
| Wedge-signature surfacing beyond cache defeat: ordinary payload-read all-shapes-24/00 storms feed the same classifier | Fork engine/App.Core; policy-level, unit-testable |
| Per-drive incident store (per-drive-identity records: timestamp, trigger context counters, rungs attempted, curing rung) | App.Core beside DriveCalibration; source-generated JSON per the store precedent |
| Recovery dialog with app-verified physical rungs (cable, then power) | Avalonia; unprivileged verification = re-enumeration watch + TOC probe, the exact probes hand-run on 2026-08-14 |
| Lead-with-known-cure policy: after the same rung cures twice consecutively, the dialog leads with it and offers "skip to what worked before"; the full ladder stays reachable | Policy class + tests |
| "Retry now" as a fresh operation; failed transaction never resumes | Wiring only; fail-closed doctrine unchanged |

Build order: classifier surfacing + incident store + policy (fork, all
testable without hardware), then the dialog, then a live wedge session
for end-to-end evidence (requires the wedge to reproduce; the damaged
reference disc on a USB drive is the proven trigger).

## 4. Out of scope, on purpose

| Excluded | Why | Log entry |
| --- | --- | --- |
| Software reset rungs (SCSI device/host reset, USB port reset) | All proven useless on the characterized hardware, and all require privileges the app does not have and will not ask for | D-060 |
| Automatic operation resume after cure | The failed transaction's independence is unprovable; fail-closed stands. Retry is a fresh run | D-062 |
| sudo/root helper processes | Never ask the user for elevation | D-060 |
| WPF head dialog | Same extension point as SLICE-010: VM/state shared, controls ported later | D-060 |

## 5. Stubs and their debts

None planned. If the live-evidence session cannot reproduce the wedge,
the dialog ships with unit/headless evidence plus a recorded Open item
for the live pass; that gap, if taken, gets its own log entry.

## 6. Modules touched

Fork: CUETools.Ripper.SCSI (signature surfacing), Bwg.Scsi (one buffer
ioctl helper), CUETools.App.Core (incident store, recovery policy,
probe seam and its Linux implementation, ladder state machine,
RipService detection, RipViewModel hooks). Linux app: the recovery
dialog only. All through existing seams; no engine retry-policy
changes.

The probe and watcher moved from the Linux app to the fork after the
increment-3 mapping pass; see decision D-066 for the reasoning and its
Proposed status. The dialog itself remains Linux-side as planned.

## 7. Data subset

New per-drive incident records in the app data directory beside the
calibration store. Contract-stable machine artifact; no album or disc
content, hardware identity and counters only (scrub rule holds).

## 8. Acceptance criteria

| ID | Criterion | Verified by |
| --- | --- | --- |
| S11-001 | The stuck-drive failure offers recovery; the dialog names the drive and shows history when present | Headless UI test + live session |
| S11-002 | Rung verification is real: the app detects re-enumeration and probes the TOC; a still-wedged drive advances the ladder with an honest message | Live wedge session (unit-tested probe logic) |
| S11-003 | The incident record is written with rungs attempted and curing rung; a second incident on the same drive leads with the known cure after two consecutive same-rung cures | Store + policy unit tests |
| S11-004 | The failed operation is never resumed; "Retry now" starts a fresh calibrated run | Code inspection + live session |
| S11-005 | An uncured ladder ends in the honest terminal state and records the incident as uncured | Headless test |
| S11-006 | A payload-read wedge (outside cache defeat) reaches the same classification and dialog | Policy unit test |
| S11-007 | Transferred from SLICE-009: a damaged-disc Test & Copy whose reads mismatch invokes the tie-break third read, with its CRC columns and resolution visible | Live session (Stop off) |
| S11-008 | Transferred from SLICE-009: a post-Copy confirmation failure lands the staged set in the explicit Held state with both reads retained and paths reported | Live session, opportunistic |

## 9. Verification evidence required

- [x] Policy, store, and probe unit tests passing in CI (fork suite,
  2026-08-15), plus the dialog walkthrough driven by a scripted probe on
  the Linux head (six tests, 2026-08-18): rung wording, advancement on a
  failed rung, cure with retry, the proven-cure lead after two
  consecutive cures, the uncured terminal with its recorded incident,
  and the unidentifiable-drive hand-instructions path.
- [ ] One live wedge session end to end (grind, dialog, verified rungs,
  cure, incident record, retry), counters recorded in this brief.
- [x] Scrub audit of the incident record and dialog text, 2026-08-18.
  The incident record carries five fields: TimestampUtc, Trigger (one of
  the two classifier names), TriggerContext (the scrubbed reader counters
  RipService stamps, which by the scrubbed-logging rule carry command
  shapes and counts, never payload), RungsAttempted (rung names), and
  CuringRung (a rung name or empty). The store key is the normalized
  drive signature, hardware identity only. The dialog's text shows the
  drive letter, rung instructions, and rung titles; the fingerprint holds
  vendor, model, revision, serial, and sr node. No field anywhere in the
  path can carry an album, artist, track, or file name. Verified by
  reading every assignment into DriveRecoveryIncident and every TextBlock
  in DriveRecoveryDialog.

## 10. Agent guardrails for this build

- **Boundary:** modules in section 6 only; no retry-policy or sense
  classification changes; no elevation of any kind.
- **Stop and ask before:** new dependencies, any change to the fatal
  fail-closed semantics, anything touching the calibration store's
  existing contract.
- **Conflicts:** if live evidence contradicts the ladder design (a
  drive that cures on a software rung, a cable-replug cure), stop and
  record it; the ladder follows evidence, not the plan.

## 11. Slice definition of done

- [ ] All acceptance criteria pass with linked evidence.
- [ ] Documents updated; incident-store contract recorded.
- [ ] Human walkthrough completed and approved by the owner.

## 12. What this unlocks

- WPF parity for the dialog (shared state, ported control).
- Longer term: the incident dataset across drives feeds any future
  conversation about firmware-specific carve-outs with real numbers.

---

*Approved for build by: Daniel Boyd, 2026-08-15 (D-064). The live
wedge session is owner-scheduled; everything before it builds under
the standing autonomy grant.*
