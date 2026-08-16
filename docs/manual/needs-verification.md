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

Status: unresolved. The opening claim reads as if it covers all modes. Verify
against the rip pipeline (SLICE-009) whether calibration and cache defeat run
in every mode or only in Secure and Paranoid, then scope the opening claim to
what the code does.

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

Status: needs a live confirmation. `pages/verify.md` states it, because
the code path is unambiguous, but no recorded run lists a `.toc` next to
a verified album. Run one verify, list the folder, and either confirm
the page or correct it. Worth an owner opinion too: a `.toc` file
appearing next to every verified album may be unwanted.

## 10. `.m3u8` playlists are accepted but probably cannot be verified

`VerificationSourceDiscovery.cs:423-424` accepts `.m3u` and `.m3u8`, and
the Verify file picker offers both. The engine only recognises `.m3u`:
`CUESheet.cs` compares extensions against `".m3u"` exactly at lines 820,
859, 1195, and 4608.

Status: unresolved, and it looks like a real defect rather than a
documentation problem. Load an `.m3u8` playlist and record what happens.
`pages/verify.md` currently promises only `.m3u`. If the engine cannot
open `.m3u8`, either teach it the extension or stop accepting it in
discovery, so the picker does not offer a file that fails later.

## 11. "not found" and "not in database" hide lookup failures

`VerifyViewModel.CtdbText` renders `not found` whenever `CtdbTotal` is
zero, and `ArText` renders `not in database` whenever `ArTotal` is zero.
A lookup that errored produces the same zero. The evidence screenshot
`docs/evidence/2026-08-11-verify-fixture-dark.png` shows exactly this:
the CTDB panel reads `not found` while the message line underneath reads
"CTDB: database access error: There is an error in XML document (0, 0)"
(the O-001 quirk).

Status: documented rather than fixed. `pages/verify.md` tells the reader
to check the message line, and its troubleshooting section covers the
case. The product question stands: the panel arguably should say
"lookup failed" rather than "not found", which would let the manual drop
the caveat.

