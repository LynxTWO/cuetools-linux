# CUETools Linux Slice Brief: SLICE-012 CTDB submission

Version: 0.1. Date: 2026-08-15. Status: Proposed, pending owner
decisions D-069 to D-071 below.
Companion documents: ARCHITECTURE.md, DECISION-LOG.md, and
`docs/manual/pages/verify.md` (the manual text that changes when this
ships).

Raised on 2026-08-15 when a manual fact-check found that verifying an
unknown pressing contributes nothing back to CTDB. The owner asked for
submission "just as it is with the Windows version." Investigation
changed the shape of that request; see section 2.

---

## 1. What the slice proves

- **Capability added:** a verified or repaired album can be submitted
  back to the CUETools Database, so an unknown pressing becomes a known
  one for the next person who checks it.
- **The slice in one line:** after a completed verify (or a published
  rip), the app offers to submit the disc's checksums and parity to
  CTDB, asks first, remembers the answer, and reports what the server
  said.
- **Honest stakes:** every album the user checks today is a one-way
  read. The database that made verification possible gets nothing back,
  and rare pressings stay unknown forever.

## 2. The parity finding (this is not a Linux gap)

Measured 2026-08-15 by source scan of the pinned fork tree:

| Head | Submits to CTDB? | Evidence |
| --- | --- | --- |
| Classic CUETools (WinForms) | Yes | `CUETools/frmCUETools.cs:1045` calls `cueSheet.CTDB.Submit(...)` |
| Classic CUERipper | Yes | `CUERipper/frmCUERipper.cs:574` |
| EAC plugin | Yes | `CUETools.CTDB.EACPlugin/FormSubmitParity.cs:55` |
| CUETools 2026 WPF | **No** | no `Submit` call in `CUETools.Wpf/` |
| CUETools Linux | **No** | no `Submit` call in `CUETools.App.Core/` or `src/` |

The submit capability exists in the engine (`CUEToolsDB.Submit(int
confidence, int quality, string artist, string title, string barcode)`)
and is consumed only by the classic heads. Neither modern head calls
it. So this is a modern-head gap, not a Linux port gap, and the work
belongs in `CUETools.App.Core` where both heads pick it up.

That also means the Windows 2026 build gains the feature from this
slice. Treat it as a fork-first slice with a Linux consumer, the same
shape as the rip extraction work.

## 3. The consent model already exists upstream

The classic head's flow, which this slice should follow rather than
reinvent:

1. `Config.advanced.CTDBAsk` gates a consent dialog (`frmSubmit`) with
   a "remember my answer" checkbox.
2. The answer is written back to `Config.advanced.CTDBSubmit`, and
   `CTDBAsk` is set false, so the question is asked once.
3. On consent, `Submit(WorstConfidence, quality 100, artist, title,
   barcode)` runs and `CTDB.SubStatus` is appended to the status text.

Both settings keys already round-trip through the Linux settings store,
so no settings format change is needed.

## 4. What is sent, and what the manual must then say

`DoSubmit` uploads the disc's TOC, CRC32 and track CRCs, a parity
syndrome, and the metadata fields passed in (artist, title, barcode).
Per `docs/manual/notes/install.md`, CTDB submissions also carry a
per-machine identifier derived from the hardware, so repeated
submissions from one machine can be recognised.

When this ships, three manual texts change:

- `pages/verify.md`, the `not in database` row, currently reads "That
  is not an error; nobody has checked this pressing before you." It can
  then say the user's own submission makes the pressing known.
- `pages/verify.md` gains a submission section (what is sent, how to
  turn it off).
- `notes/install.md` "What leaves your machine" gains the submission
  trigger, since the current text describes submissions without saying
  what causes one.

## 5. Owner decisions needed

- **D-069 - when is submission offered?** After a completed verify,
  after a published rip, both, or only from an explicit button. The
  classic head submits at the end of a processing run.
- **D-070 - default answer.** Ask once and remember (classic
  behavior), or off until the user turns it on in settings. Linux has
  no settings page yet, which argues for ask-once.
- **D-071 - what quality value.** The classic head hardcodes `quality:
  100`. Confirm that is right for a modern secure rip, and what a
  Salvage-quality or partially unrecoverable rip should send, if
  anything.

Additional constraint to confirm: whether a rip with unrecoverable
windows (`CONSISTENT`, salvaged, or held) may be submitted at all. The
conservative default is no.

## 6. Acceptance rows (draft)

- S12-001: consent dialog appears once, records the answer, and never
  submits without an explicit yes.
- S12-002: a submitted disc is retrievable from CTDB afterwards by TOC
  id, proven by a fresh lookup from a clean profile.
- S12-003: declining leaves zero outbound submission traffic, proven
  by a network capture.
- S12-004: a failed submission degrades to a status message and never
  changes the verify verdict.
- S12-005: the manual texts in section 4 are updated in the same
  batch.

## 7. Why this is not started yet

Submission is an outward-facing, effectively irreversible action: it
publishes the user's disc identity and metadata to a public database.
The decisions in section 5 change what the consent text must say, so
the design is owner input, not an implementation detail. Build starts
once D-069 to D-071 are recorded.
