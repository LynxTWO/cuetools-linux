# Manual: needs verification

Claims in `notes/` that conflict with each other or have not been checked
against the current app. A claim listed here stays out of user-facing manual
pages until it is verified against the running app, the source, the tests, or
command help. Do not resolve an entry by picking the claim that sounds more
plausible; verify it, fix the note, then remove the entry.

Opened 2026-08-15 from the manual writing review. Line numbers refer to the
working tree on that date.

## 1. Rip: "every rip is secure" vs the Quality modes

- `notes/rip.md` lines 5-11 say every rip is a secure read and "There is no
  fast-but-unverified mode hiding behind a setting."
- `notes/rip.md` lines 27-33 list Quality modes Burst, Secure, Paranoid, and
  Salvage. Burst is "a single pass with the historical retry cap"; Salvage
  captures "at Burst quality with error pointers off."

Status: RESOLVED 2026-08-16 against the code. The note's "every rip is a
secure read" is false, and the part that is true of every mode is
calibration, not secure reading.

`RipService.RunVerify` (line 365), `RunEncode` (387), and `RunTestAndCopy`
(1623) all call `EnsureCalibration`, so Burst, Secure, Paranoid, Salvage,
and Test and Copy alike calibrate the drive before touching the disc.
What varies is the independent re-read requirement: Verify and Rip pass
`requireIndependentReads: cq > 0`, so it binds only above the lowest
correction quality, while Test and Copy passes `true` unconditionally.

`pages/rip.md` states the resolved version. The note still carries the
absolute claim and should be corrected to match.

## 2. Verify vs Codecs: WavPack and APE availability

- `notes/verify.md` lines 18-20 say sources needing native codecs (WavPack,
  APE, TTA) show a "codec unavailable in this build" row "until the codec
  runtime ships."
- `notes/codecs.md` lines 10-11 list WavPack and Monkey's Audio (APE) as
  "native, vendored."

Status: unresolved. verify.md predates the codec runtime work (SLICE-005) and
is probably stale, but that is inferred, not verified. Confirm in the current
build which formats Verify accepts, then update verify.md. TTA is claimed by
neither note; check it separately.

## 3. Install: is the network claim complete?

- `notes/install.md` ("What leaves your machine") lists AccurateRip lookups,
  CUETools Database requests and submissions, and cover art fetches, and says
  local work sends nothing.
- `notes/rip.md` line 16 says the album and track list come from MusicBrainz.
  MusicBrainz is not in the install.md list.

Status: unresolved. Enumerate every outbound endpoint (MusicBrainz, cover art
hosts, AccurateRip, CTDB) from the source and add the missing ones. Decide
whether the page should also state the stronger claim explicitly: no
telemetry, no update checks, network traffic only for the features that need
it.

## 4. "Bit-exact" needs an object

- `notes/convert.md` line 35 and `notes/queue.md` line 38 call a lossless
  conversion "bit-exact."

Status: needs precision, not a conflict. AccurateRip and CTDB compare decoded
PCM, so the verified claim is "the decoded audio is bit-identical to the
source"; the encoded files are not identical. Manual pages must say what is
being compared.

## 5. AppImage: "other distributions" vs the glibc floor

- `notes/install.md` presents the AppImage as the path for "other
  distributions."
- The same note's system requirements (working tree, 2026-08-15) say the
  binary needs glibc 2.38 or newer, which excludes Ubuntu 22.04 and Debian 12.

Status: partially resolved in the working tree. Keep the two sections in
agreement: the AppImage requirement is the glibc floor, not "any distro."
Update this entry when the intended glibc floor (2.35/2.36) ships.

## 6. CTDB "no exact match | N entries": which causes may the manual name?

RESOLVED 2026-08-15. `CUEToolsDB.cs` calls `verify.FindOffset(...)` per
entry while matching, so CTDB locates the read offset itself. A rip made
without offset correction is therefore not a cause of this row, and the
manual says so in "How it works". The two causes the page may name:

- A different pressing or master sharing the same track layout. Live
  example: the owner's 24-track disc read byte-identically on two drives
  (2026-08-14 disc-swap experiment) and still matched nothing
  (AccurateRip 0/82, CTDB 0/235).
- Damage past what the stored parity can rebuild (`hasErrors` with
  `canRecover` false).

## 7. Verify: report rewrite vs preserved history

`notes/verify.md` said "the app never rewrites one [a report]". The code
disagrees: `src/CUETools.Linux.App/Journal/VerificationBackfillService.cs`
snapshots the prior report to `<name>.accurip.<timestamp>.pre-backfill`
only on the automatic backfill path, and an ordinary re-verify writes the
same report filename again.

Status: RESOLVED 2026-08-15 for the manual. `pages/verify.md` states the
source-verified behavior, and the note was corrected to match. Left here
as the record of the correction.

## 8. Verify: the "codec unavailable in this build" row

`notes/verify.md` quoted a UI row reading "codec unavailable in this
build". That exact string exists nowhere in the app source; the only
similar text is the diagnostic log line "native codec unavailable:" in
`src/CUETools.Linux.App/Services/NativeCodecLoader.cs:83`.

Status: unresolved, and related to entry 2. Before any page describes
what a reader sees when a format cannot be decoded, run the case and
record the actual on-screen text. The Verify page currently avoids the
claim entirely and points at the codecs page instead.

## 9. Does every verify write a `.toc` file?

Traced in code 2026-08-15, not yet seen in an evidence run.
`Composition.cs:120` sets `config.advanced.CreateTOC = true`, and
`VerifyService.TrySetVerifyLogTarget` gives the sheet an output path
rooted at the source, so the `CreateTOC` branch in `CUESheet.cs:3451`
(guarded by a non-empty `OutputDir` and non-null `_outputPath`) should
fire on every CUE-based verify and write `<name>.toc` beside the album.

Status: RESOLVED 2026-08-17 by running it. An album folder was staged
holding only `.flac` and `.cue`, one verify was run against it, and both
files appeared:

```text
Steely Dan - Aja (1977).accurip     2624 bytes
Steely Dan - Aja (1977).toc          558 bytes
```

The `.toc` is a human-readable track table (Track, Start, Length, Start
sector, End sector), identical in shape to the one a rip writes.
`pages/verify.md` was right on code reading alone, and is now measured.

The same run confirmed two more page claims. The report's first line
reads `[CUETools log; Date: 08/17/2026 07:36:15; Version: 2.2.6]`, so the
version really is the engine's rather than the Linux app's, and the
second line reads `[CTDB TOCID: r81hxErrwZPVmTUZigpDmIQyQqo-] found.`,
confirming the id on a disc card is CTDB's.

The open owner question stands and is now a product decision rather than
a documentation one: a `.toc` appearing beside every verified album, not
just every rip, may be unwanted. Recorded as F-37 in
`docs/FINDINGS-2026-08-16-manual-pass.md`.

## 10. `.m3u8` playlists are accepted but probably cannot be verified

`VerificationSourceDiscovery.cs:423-424` accepts `.m3u` and `.m3u8`, and
the Verify file picker offers both. The engine only recognises `.m3u`:
`CUESheet.cs` compares extensions against `".m3u"` exactly at lines 820,
859, 1195, and 4608.

Status: RESOLVED 2026-08-16 by fixing the app (D-071). The engine was
the problem, and `CUESheet.IsPlaylistExtension` now answers for both
extensions at all four comparison sites. Covered by
`PlaylistExtensionTests` in the fork and
`LookupStatusAndPlaylistTests.Utf8PlaylistsAreAcceptedByDiscoveryAndTheEngine`
on the Linux side. The manual may promise `.m3u8` from the pin bump
that carries the fix.

## 11. "not found" and "not in database" hide lookup failures

`VerifyViewModel.CtdbText` renders `not found` whenever `CtdbTotal` is
zero, and `ArText` renders `not in database` whenever `ArTotal` is zero.
A lookup that errored produces the same zero. The evidence screenshot
`docs/evidence/2026-08-11-verify-fixture-dark.png` shows exactly this:
the CTDB panel reads `not found` while the message line underneath reads
"CTDB: database access error: There is an error in XML document (0, 0)"
(the O-001 quirk).

Status: RESOLVED 2026-08-16 by fixing the app. `VerifyFilesResult` now
carries `ArLookupFailed` and `CtdbLookupFailed`, set when the status is
anything other than success or a genuine 404, and the panels say
`lookup failed`. Covered by `LookupStatusAndPlaylistTests`, including
the case where a database answered before failing: a real verdict still
wins over the failure wording. The manual's caveat comes out with the
pin bump that carries the fix.

## 12. "Selected files must belong to the same album location" may be unreachable on Linux

`VerificationSourceDiscovery.cs:121` raises this when the selected paths
have more than one distinct `Path.GetPathRoot`. On Linux every absolute
path shares the root `/`, so a selection spanning two folders, or even
two mounted disks, does not trigger it. The message that actually fires
when a folder is mixed with other items is "Drop one album folder at a
time, or select manifest files from one album."

Status: RESOLVED 2026-08-16 by fixing the app. The path-root test is
kept, because it does real work on Windows, and a second test now
rejects a selection whose nearest common ancestor is the filesystem
root, which is reachable on any platform. Covered by
`LookupStatusAndPlaylistTests.FilesFromUnrelatedFoldersAreRejectedOnLinuxToo`,
which also pins the case that must keep working: two folders under a
shared parent are a legitimate multi-disc selection.

## 13. The 2026-08-12 repair was probably not at the limit of what parity could do

`notes/repair.md` said the walkthrough disc "sat at the exact limit of
what npar=16 parity can recover" and that one more damaged sector in that
stripe would have defeated the repair. The code disagrees.
`CUETools.CTDB/CUEToolsDB.cs:474-490` fetches parity at increasing depth
(4, then 8, then 16, capped at the entry's own `Npar` and at
`AccurateRipVerify.maxNpar`) and stops at the first depth that recovers,
while `CUETools.AccurateRip/CDRepair.cs:182` sets the fix's stripe
capacity to the fetched depth divided by two. A reported capacity of 4
therefore means the fix succeeded at depth 8, not at 16.

Status: the headline claim is RESOLVED 2026-08-17 by code, with one
narrower question left open. The chain was traced end to end and needs no
live repair:

1. `CUEToolsDB.cs:474` walks `npar = 4, 8, 16`, capped at the entry's own
   `Npar` and at `AccurateRipVerify.maxNpar`, and breaks at the first
   depth that recovers (lines 483-490).
2. The fetched syndrome's width is authoritative, not the loop variable:
   line 482 reassigns `npar = syn.GetLength(1)`, and
   `CDRepair.cs:170` independently rederives it as `syn2.GetLength(1)`.
3. `CDRepair.cs:182` then sets `columnCapacity = npar / 2`.

`maxNpar` is 16 (`AccurateRip.cs:18`), so the `Math.Min` on line 171 never
clamps below the ladder. Stripe capacity is therefore always exactly the
fetched depth divided by two. A capacity of 4 means the fix ran at depth
8, and "the exact limit of what npar=16 parity can recover" is wrong for
any disc, not just this one.

Still open, and narrower than the original entry: whether depth 16 was
*available* for that particular disc. The ladder is capped at the CTDB
entry's own `Npar`, so if that entry carried only npar=8, then 4 of 4 was
its true ceiling after all and one more damaged sector really would have
defeated the repair. Nothing in the repository records that entry's
`Npar`, so it stays unknown for the 2026-08-12 disc specifically.

That gap is now self-closing. `VerifyService` logs both depths on every
repair (`fix/parity-depth-evidence`, commit 18d59e77): `ctdb parity: entry
npar=N, fix ran at npar=M`, plus whether deeper parity remained. The next
repair on any disc settles it without a special session.

The manual states the two numbers and what they count, and makes no claim
about what one more damaged sector would have done. Both notes are
corrected.

Not settled by the 2026-08-17 bad-master disc, and it never could be:
damage that dense is far past what parity repairs, so no repair runs and
no depth is ever fetched. See F-40.

Related product finding: F-19 and F-20 in
`docs/FINDINGS-2026-08-16-manual-pass.md` cover the same mismatch on the
panel and in the R115 doc comment.

## 14. Walkthrough figures with no surviving receipt

Three figures from the 2026-08-12 repair walkthrough cannot be confirmed
from anything in the repository:

- "129 of 307,669 sectors" and the affected ranges "41:29 to 66:37"
  (`notes/repair.md`). `VerifyFilesResult.RepairTotalSectors` is recorded
  in the receipt but never displayed, no receipt from that run is checked
  in, and the ranges string in
  `docs/evidence/2026-08-12-repair-real-disc-repairing.png` is truncated.
- "Track 1 took 47 seconds against 3-30 seconds for the rest" and "88
  seconds from confirmation to published". No log or receipt records
  either.

Status: unresolved. Re-run the walkthrough and keep `repair.verify`, or
drop the figures. The manual uses only the corrected-sample and
corrected-sector counts, which `docs/SLICE-002-repair.md:93` corroborates.
A reader comparing their own run against an unverified timing would draw
the wrong conclusion about their drive.

## 15. The RepairScope states screenshot shows impossible figures

`docs/evidence/2026-08-12-repairscope-states.png` reads "18,342 samples
across 12 sectors" in two of its three rows. That cannot come from a real
fix: `CDRepairFix.CorrectableErrors` counts 16-bit values and
`AffectedSectorArray` indexes CD sectors
(`CUETools.AccurateRip/CDRepair.cs:308-322`), and 12 sectors hold only
12 x 588 x 2 = 14,112 such values.

Status: RESOLVED 2026-08-17 by labelling it. The arithmetic settles it
without a rerun: `CorrectableErrors` counts 16-bit values and 12 sectors
hold only 12 x 588 x 2 = 14,112 of them, so 18,342 cannot come from a
real fix. `notes/repair.md` now says the figures are an example render,
and the manual page's caption already framed them that way and quotes
none of them.

Owner's plan (2026-08-16): redo the manual screenshots from real discs
eventually, substituting names where a real album should not appear. That
is a separate piece of work, not a blocker on any page.

## 16. install.md describes a submission identifier for submissions that never happen

`notes/install.md` ("What leaves your machine") says "Database
submissions include a per-machine identifier derived from your hardware,
so repeated submissions from one machine can be recognised." Verified
2026-08-15 and again 2026-08-16: nothing in either modern head calls the
CTDB client's `Submit`, so no submission occurs at all.
`CUETools.App.Core/Services/CtdbSubmission.cs` is eligibility policy with
no consent surface behind it.

Status: RESOLVED 2026-08-18. SLICE-012 shipped: the Linux head now calls
`Submit` after an explicit yes to a consent dialog, and the privacy
sections in `pages/install.md` and `notes/install.md` describe the real
payload, the per-machine identifier included. The claim is no longer
about a path that does not run.

Backed by a live submission the same day: the server confirmed the disc's
TOCID in the redacted diagnostic log, and an independent raw lookup
showed the matching entry's confidence rise from 796 to 797 while the
variant pressings stayed unchanged (S12-002).
