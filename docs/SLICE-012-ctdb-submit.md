# CUETools Linux Slice Brief: SLICE-012 CTDB submission

Version: 0.2. Date: 2026-08-16. Status: Approved for build (D-069,
D-070, recorded 2026-08-16). The policy and service are built; the
consent surface and the live evidence run are the remaining work.
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

## 5. Owner decisions, recorded 2026-08-16

- **D-069: ask once, then remember.** The first eligible disc after a
  completed verify or a published rip raises one consent dialog with a
  remember checkbox, writing `advanced.CTDBSubmit` and clearing
  `advanced.CTDBAsk`. Nothing uploads before an explicit yes.
- **D-070: only reads without unrecoverable errors.** Salvaged output,
  held Test and Copy results, and rips carrying unrecoverable windows
  are ineligible whatever the databases said. Eligible discs send the
  classic quality value of 100. A real quality value computed from rip
  evidence is a named follow-up, not part of this slice.

A failed lookup also blocks submission: when the database never
answered, its view of this disc is unknown, and submitting risks
duplicating an entry it already holds. That falls out of the same fix
that separated `lookup failed` from `not found`.

## 5a. What is built

`CUETools.App.Core/Services/CtdbSubmission.cs`:

- `CtdbSubmissionPolicy` holds the whole decision with no I/O:
  eligibility, the ask-once gate, and the remembered answer. A quality
  block outranks a remembered yes, so a salvaged rip stays ineligible
  for a user who once said "always submit".
- `CtdbSubmissionService` runs policy, then consent, then the upload,
  and swallows submission failures so a verify or rip verdict never
  changes because a database was unreachable. It redacts the album,
  artist, and barcode from the diagnostic log before the server's echo
  is written.
- `ICtdbSubmissionPrompt` is the consent seam. **A head with no
  implementation cannot submit**, which is the shipped state today: no
  head implements it, so nothing can upload by accident.

Tests: `CtdbSubmissionPolicyTests` (11) pins every block and both
remembered answers.

## 6. Acceptance rows (draft)

- S12-001: consent dialog appears once, records the answer, and never
  submits without an explicit yes. DONE 2026-08-18: shown live on a real
  verify; twelve unit tests pin the ask-once flow, both remembered
  answers, and every eligibility block; the shipped run asked exactly
  once and uploaded only after Share was clicked.
- S12-002: a submitted disc is retrievable from CTDB afterwards by TOC
  id, proven by a fresh lookup from a clean profile. DONE 2026-08-18:
  after one live submission (Steely Dan - Aja, AR 205/216), the server
  replied "TOCID has been confirmed" into the redacted diagnostic log,
  and a raw lookup2.php GET with no app state showed the matching entry
  at confidence 797, up from 796 before the click, total 821 from 820,
  variant pressings unchanged. The two dialog deadlock bugs found on the
  way (a preview without an event loop, then Ask blocking the UI thread
  under ShowDialog) are fixed and pinned by tests.
- S12-003: declining leaves zero outbound submission traffic, proven
  by a network capture. OPEN: unit tests prove declining returns before
  the upload call, but the capture-level proof has not been run.
- S12-004: a failed submission degrades to a status message and never
  changes the verify verdict. DONE in tests 2026-08-18, including the
  case where the engine refuses to send (never-queried database): the
  outcome says "Could not share" rather than claiming success.
- S12-005: the manual texts in section 4 are updated in the same
  batch. DONE 2026-08-18: pages/install.md gained "Sharing a rip with
  CTDB, if you say yes" and a corrected "What never goes out";
  notes/install.md carries the payload list and the live evidence;
  needs-verification entry 16 is closed by it.

## 7. What remains, and why it waits

Submission is an outward-facing, effectively irreversible action: it
publishes the user's disc identity and metadata to a public database.
The policy is settled and built. What remains:

1. **The consent dialog**, one per head (Avalonia and WPF). Its text
   must name what is uploaded: the disc's table of contents, the track
   checksums, parity data, the artist and title, the barcode, and the
   per-machine identifier the CTDB client already sends.
2. **Wiring the service into the verify and rip completion paths.** The
   database object has to be the live one from the run that produced
   the result, because its parity and checksums are what upload; a
   submission cannot be replayed later from a stored verdict.
3. **The live evidence run (S12-002).** A real submission to the public
   database cannot be undone, so it happens with the owner present, on
   a disc chosen for the purpose, and is confirmed by a fresh lookup
   from a clean profile.

Until step 1 lands, `ICtdbSubmissionPrompt` has no implementation and
nothing can upload.
