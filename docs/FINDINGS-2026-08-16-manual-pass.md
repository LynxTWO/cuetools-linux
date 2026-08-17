# Findings from the manual rewrite pass, 2026-08-16

Writing the user manual meant tracing every claim to source. That turned
up 22 things the code does that the notes did not describe, or that look
like defects rather than documentation problems. They are recorded here
rather than in `docs/manual/needs-verification.md`, which is for
conflicting or unverified *documentation* claims.

Each entry carries the evidence found at the time. None is fixed unless
its status says so. Severity is this reviewer's judgement, for the owner
to confirm.

Method note: these came from agents writing the pages plus an adversarial
pass over each finished page. Several entries record a live reproduction
run on 2026-08-16 against the production service, not just a code read.

---

## Serious: recorded evidence can be wrong or lost

### F-01 Backfill snapshots the wrong disc's report (multi-disc albums)

`VerificationBackfillService.FindFreshReport`
(`src/CUETools.Linux.App/Journal/VerificationBackfillService.cs:100-117`)
returns the newest `*.accurip` in the album's folder, not the report for
the disc being replayed. Called at `:63` it can snapshot a different
disc's report to `.pre-backfill` while the replayed disc's own report is
overwritten with no copy kept. Called at `:82` it can record another
disc's report as the entry's `ResolutionEvidencePath`. A multi-disc album
in one folder hits this every time.

Severity: high. This is silent evidence loss in the one path whose job is
preserving evidence.

FIXED and demonstrated 2026-08-17. Two real albums were staged in one
folder as `disc1.cue` and `disc2.cue` and verified by the app, which
wrote `disc1.accurip` and `disc2.accurip`: the engine names a report from
the source stem, which is exactly what the fix computes. With
`disc2.accurip` made the newer file, replaying the `disc1` entry selects
`disc2.accurip` under the old rule and `disc1.accurip` under the new one.

Worth recording for severity: this repository's own box-set rips go into
one folder per disc, so no rip layout here ever hit it. The layout that
does is several discs sharing one folder, which discovery explicitly
supports (`DistinctCueSheetsInOneFolderRemainSeparateDiscs`) and which a
verify of an existing library commonly has.

### F-02 A network drop mid-replay marks entries resolved with an error inside

`VerificationBackfillService.cs:78-86` marks an entry Resolved on
`result.Ok` alone, and `_isOnline()` is checked once for the whole batch
at `:37`. A network that drops after that probe produces fresh reports
containing only `database access error`, marked resolved, never retried.

Severity: high. The queue's purpose is to catch up later; this quietly
consumes the entry.

### F-03 Two windows can replay the same albums at once

Backfill replay starts from every window's `Opened` handler, including
secondary drive windows (`src/CUETools.Linux.App/App.axaml.cs:245`
ignores `launchOptions.IsSecondaryDriveWindow`). `JournalStore` has no
cross-process lock, so two windows launched together read the same
pending entries and re-verify the same albums concurrently.

Severity: high. Contrast with the optical-drive lease, which is
explicitly cross-process; the journal has no equivalent.

### F-04 Offline verifies queue duplicate pending entries

`src/CUETools.Linux.App/Journal/JournalingVerifyService.cs:34-40` calls
`CreatePending` on every offline verify with no already-pending check, so
verifying one album offline N times queues it N times and the next launch
re-verifies it N times. The enrichment lane does dedupe
(`Services/EnrichmentService.cs:115-121`), and its comment claims "Same
double-check discipline as the verify lane", which is not what the verify
lane does.

Severity: medium. The comment asserting parity that does not exist is
worth fixing along with the behavior.

---

## The lookup-failed fix did not reach every surface

Today's change taught `VerifyViewModel` to distinguish a failed lookup
from an absent disc. Two other surfaces still collapse them.

### F-05 The Rip page still says "not in database" for a failed lookup

`extern/cuetools_2026/CUETools.App.Core/ViewModels/RipViewModel.cs:2199-2204`
renders `not in database` and `not found` purely from zero totals, with no
lookup-failed branch, so the fix applied to `VerifyViewModel.ArText` and
`CtdbText` (`:498-509`) did not reach the rip verdict panels.

Severity: medium. Same defect the Verify page just had, in the surface
where a user is most likely to be offline mid-rip.

### F-06 The Queue reports a failed lookup as "No match"

`QueueViewModel.cs:268` maps any `Ok` verify with no confirmation to
`No match` and never reads `VerifyFilesResult.ArLookupFailed` or
`CtdbLookupFailed`
(`extern/cuetools_2026/CUETools.App.Core/Services/VerifyService.cs:52-53`).

Severity: medium. "No match" is a claim about the audio; the database may
never have answered.

### F-07 Convert and Queue file pickers still exclude .m3u8

`CUETools.App.Core/ViewModels/ConvertViewModel.cs:212` and
`QueueViewModel.cs:173` both declare
`new("Rip sets", new[] { "cue", "m3u" })`, while `VerifyViewModel.cs:117`
was updated to
`new("Rip sets (*.cue, *.m3u, *.m3u8)", new[] { "cue", "m3u", "m3u8" })`.
`CUESheet.IsPlaylistExtension` (`CUETools.Processor/CUESheet.cs:2026`)
accepts both, and a 2026-08-16 probe converted an `album.m3u8`
successfully. A user with a UTF-8 playlist must switch the picker to "All
files" to see a file the app can convert.

Severity: low, but it is an inconsistency introduced by today's change and
should land with it.

---

## Broken or misleading controls

### F-08 Convert and Queue offer album folders the engine cannot open

`src/CUETools.Linux.App/Views/ConvertView.axaml:39` is a **Folder...**
button, `CUETools.App.Core/ViewModels/ConvertViewModel.cs:228` titles its
picker "Choose an album folder to convert", and `ConvertView.axaml:68`
advertises "an album folder" in the page body.
`CUETools.App.Core/Services/ConvertService.cs:119` passes the path
unchanged to `CUESheet.Open`, which throws at
`CUETools.Processor/CUESheet.cs:1116` with `"is a directory"`.

Reproduced 2026-08-16 against the production `ConvertService`: a folder
containing `album.cue` and two WAV tracks returned
`ok=False err='is a directory'`, surfacing as `Convert failed: is a
directory`. Queue has the same defect (`QueueViewModel.cs:190` adds a
folder, `:258` converts it). Verify does not, because
`VerificationSourceDiscovery` resolves a folder to its manifests first.

Severity: high. An advertised control fails every time. The fix probably
is to route Convert and Queue through the same discovery Verify uses.

### F-09 Stop after disc claims to stop a repair, and does not

`src/CUETools.Linux.App/Views/VerifyView.axaml:59` keeps **Stop after
disc** visible and enabled during a repair (IsVisible binds IsBusy,
CanExecute is `IsBusy && !_stopRequested`), and pressing it sets the
status line to "Stopping after the current disc. Its completed evidence
will be kept." (`VerifyViewModel.cs:298-303`). `RepairDiscAsync`
(`VerifyViewModel.cs:229-256`) never reads `_stopRequested` and
`RunServiceAsync` passes no cancellation token, so the repair runs to
completion and publishes.

Severity: medium. The user is told a stop was accepted when nothing was
stopped.

### F-10 The Repair button stays on the card during a repair

`src/CUETools.Linux.App/Views/VerifyView.axaml:166-169` binds **Repair
this disc**'s IsVisible to CanRepair, which is still true from the
pre-repair result while the repair runs (`VerifyViewModel.cs:455-456`
reads the unchanged Result). The button stays visible, greyed out,
throughout; it can be seen next to the WORKING chip in
`docs/evidence/2026-08-12-repair-real-disc-repairing.png`.

Severity: low.

### F-11 No way to stop a running batch

`RunAllAsync` (`QueueViewModel.cs:223-279`) takes no cancellation token
and `QueueView.axaml` has no stop control, while the Verify page offers
Stop after disc for the equivalent multi-disc run. A long batch can only
be ended by killing the app, which abandons a conversion's
`.cuetools-stage-` folder rather than disposing its publication
transaction.

Severity: medium.

### F-12 No per-item remove in the Queue

`RemoveCommand` exists at `QueueViewModel.cs:97`, but the row template in
`src/CUETools.Linux.App/Views/QueueView.axaml:56-77` is four TextBlocks
with no button, and nothing else binds the command. Combined with
`RunAllAsync` re-running every item from the top
(`QueueViewModel.cs:229-231`), a mistaken entry cannot be removed.

Severity: medium.

### F-13 Items added mid-run are silently excluded from that batch

`AddFilesCommand` and `AddFolderCommand` have no CanExecute guard
(`QueueViewModel.cs:95-96`), while `RunAllAsync` snapshots both the item
list and the total before the loop (`:227-229`). A row added mid-run
appears in the list, is counted in neither `[n/total]` nor the progress
bar, and stays `Pending` after the batch reports completion.

Severity: medium.

### F-14 Queued conversions ignore the chosen output folder

`QueueViewModel.cs:258` passes an empty `outputDir`, so
`ConvertService.Convert` falls back to MyMusic/CUETools every time
(`extern/cuetools_2026/CUETools.App.Core/Services/ConvertService.cs:121-123`).
The Queue page has no output-folder control of its own.

Severity: medium.

### F-15 The Enrichment pending card is silent when still offline

`src/CUETools.Linux.App/MainWindow.axaml.cs:81-84` catches
`EnrichmentOfflineException` and breaks the loop with no dialog and no
status change, so the card looks unresponsive.

Severity: low.

---

## Status and progress reporting

### F-16 The offline notice is overwritten before it can be read

`JournalingVerifyService.cs:37-39` appends "Offline: database
verification queued for automatic backfill." through the progress
callback, then `CUETools.App.Core/ViewModels/VerifyViewModel.cs:209` sets
the card's status to `result.Status` and `:223` replaces the page status
with `BuildCompletionStatus()`.

Severity: medium, and it affects the manual directly: `pages/verify.md`
tells the reader to watch the status line for that sentence. If it cannot
survive to be read, either the app should keep it or the page should stop
promising it.

### F-17 An offline batch gives no offline indication at all

The queue's `Report` handler ignores the status argument and uses only
the fraction (`QueueViewModel.cs:235-240`), so the offline sentence never
reaches the user in a batch.

Severity: medium.

### F-18 The repair progress bar restarts several times

Reported against `pages/repair.md`: the bar fills and restarts at least
three times during one repair, so it does not track the run as a whole.
Worth deciding whether the bar should span the transaction or the page
should describe the phases.

Severity: low, documentation-visible.

---

## Numbers whose meaning does not match their label

### F-19 Parity depth and stripe capacity can describe different depths

`VerifyService.cs:456` reads `repNpar = rep.Npar`, the entry's syndrome
depth from the initial query, never updated after a deeper fetch (the
assignment is commented out at `CUETools.CTDB/CUEToolsDB.cs:487`), while
`:457` reads the fix's `StripeCapacity`, which is the fetched depth / 2
(`CUETools.AccurateRip/CDRepair.cs:182`). `RepairScope.cs:120-123` then
renders "npar=16 parity symbols / 10-sector stride" beside
`VerifyViewModel.cs:530-532`'s "worst stripe 4/4", as in
`docs/evidence/2026-08-12-repair-real-disc-verified.png`, which reads as
though capacity were npar/4.

Severity: medium. Two numbers on one panel invite a comparison that is
not valid.

Partly addressed 2026-08-17. The diagnostic log now records both depths
explicitly on every repair (`fix/parity-depth-evidence`, commit 18d59e77),
so the two can no longer be confused in evidence. The panel itself still
renders them side by side without saying they are different depths, so
the rendering half of this finding stands.

### F-20 The headroom number's stated meaning is wrong in one case

The R115 doc comment at `CUETools.AccurateRip/CDRepair.cs:297-301` says
that at capacity "one more error in that stripe would have made the disc
unrecoverable, so the user can weigh re-ripping against repairing".
`CUETools.CTDB/CUEToolsDB.cs:474-490` escalates parity depth (4, 8, 16)
and only gives up when the entry's own depth is exhausted, so a fix at
capacity with `entry.Npar` above the fetched depth would have been
retried deeper.

Severity: medium. The manual must not repeat the "at the edge of
recoverability" reading until this is settled. Note the 2026-08-12
walkthrough reported exactly this state (worst stripe 4 of 4).

FIXED 2026-08-17 in the fork, and written up as F-39. The comment now
carries the depth qualifier and warns against calling a fix at capacity
one error from unrecoverable without checking which rung of the ladder it
ran at. Fixing the comment mattered on its own, because it is where the
manual's wording came from: correcting the prose alone would have let the
claim grow back from the source.

---

## Platform assumptions

### F-21 Archival encoder defaults are never applied on Linux, but the UI says they are

`EncoderCatalog.ApplyArchivalDefaults`
(`CUETools.App.Core/Services/EncoderCatalog.cs:1310`) is called only from
`extern/cuetools_2026/CUETools.Wpf/App.xaml.cs:187`;
`src/CUETools.Linux.App/Composition.cs:223-231` builds the catalog
without it. Meanwhile
`CUETools.App.Core/ViewModels/EncoderSettingsViewModel.cs:255-290`
renders hints such as "Default 8: maximum archival compression" (FLAC)
and "Default insane" (Monkey's Audio) regardless of head.

Severity: medium. Either the Linux composition applies the same one-time
defaults, or the hint text stops naming a default it did not set.

### F-22 Command-line encoders must be named `.exe` on Linux

`EncoderCatalog.IsSimpleExecutableName`
(`CUETools.App.Core/Services/EncoderCatalog.cs:1192-1195`) requires every
command-line encoder to end in `.exe`, so the Linux release ships ELF
binaries named `mpcenc.exe`, `oggenc.exe`, and `opusenc.exe` (manifest
`linux-encoders.json`). It works, and the picker rows read "Opus (.opus)
- opusenc.exe", but the suffix leaks a Windows assumption into
Linux-facing UI text and into the rule a user must satisfy when importing
their own encoder.

Severity: low, cosmetic, but it is user-visible text.

---

## Suggested order

1. F-01, F-02, F-03: evidence correctness in the backfill lane. These
   lose or corrupt the record silently.
2. F-08: an advertised control that always fails.
3. F-05, F-06, F-07: finish today's lookup-failed and `.m3u8` work so the
   app is consistent across pages.
4. F-16: decide whether the offline notice survives, because the manual
   currently promises it.
5. F-19, F-20: settle what the parity numbers mean before the manual
   explains them.
6. The rest as ordinary UX work.

---

# Second pass: Rip, Install, Codecs, Enrich, Settings

Another 37 findings from writing the remaining five pages. The ones that
change what the product promises are first.

## Serious: the app contacts the network before you ask it to

### F-23 Launching with a disc in the drive starts lookups and downloads

`RipViewModel` is constructed in `Composition.CreateAppGraph` and reads
the disc in its constructor. `ReadDiscAsync` calls `TriggerArtFetch()` as
soon as the disc is identified, and `FetchArtAsync` downloads the first
eligible front cover with no prompt. `_config.embedAlbumArt` and
`extractAlbumArt` both default to true.

So starting the app with an audio CD inserted produces a CTDB metadata
lookup, possibly a freedb lookup, a MusicBrainz query, and a Cover Art
Archive download, none of them requested.

Severity: high, and it is a documentation contradiction as well:
`notes/install.md` and the README's Privacy section both say local work
sends nothing. One of the two has to change. This is the same class of
problem as entry 3 in `needs-verification.md`, but worse, because the
traffic happens without the user choosing a feature at all.

### F-24 CTDB-supplied artwork has no host allowlist

`AlbumArtService` maps the "CTDB metadata" provider to
`ProviderPolicy.ExternalArtwork`, whose `ValidateUri` arm is `_ => true`,
so a thumbnail or master named by the CTDB response is fetched from any
public HTTPS host. MusicBrainz, Cover Art Archive, and TheAudioDB are
each pinned to their own hosts; the CTDB path is the one that is not.

The Linux Enrich path is stricter:
`EnrichmentService.TryApprovedCoverUri` allows only coverartarchive.org,
archive.org, and db.cuetools.net. That asymmetry suggests the rip path's
looseness is unintended rather than a decision.

Severity: high. A database response decides which host the app connects
to.

## Serious: Salvage does not do what it says

### F-25 Salvage is offered on three buttons and honoured by one

`RipViewModel.RunJobAsync` passes `EffectiveCorrectionQuality` (3 maps to
0) to `RunEncode` and `RunVerify`, neither of which takes a `salvage`
argument. Pressing **Rip** or **Verify only** with Salvage selected
silently runs a plain Burst job: no minimum-speed pin, no concealment,
and no `salvaged` grade on the output. Only **Test & Copy** passes
`salvage: true`.

Severity: high. The user believes they are making a salvage capture of a
failing disc and gets an ordinary Burst rip that is not labelled as one.

### F-26 Two shipped strings misdescribe Salvage, one of them into the archive

The quality tooltip (`RipView.axaml:229`) and the accept-anyway Test and
Copy log (`RipService.cs:2402`) both say Salvage turns C2 error pointers
off. `RipService.cs:515-526` keeps C2 on and sets
`ConcealUnconfirmedSamples = true` instead, and its own log line says "C2
pointers ON". A comment there records that the first build did turn C2
off and produced fourteen times a good rip's glitch rate.

Severity: high. The log travels with the audio, so the user's permanent
record of how that capture was made is wrong.

## Other findings

- **F-27** The connectivity probe ignores the profile's proxy.
  `ConnectivityProbe.IsOnline` opens raw `TcpClient` connections to
  db.cuetools.net:80 and www.accuraterip.com:443, while every real lookup
  goes through `config.GetProxy()`. On a network where direct outbound is
  blocked but a proxy works, every verify is misreported as offline,
  journaled, and the backfill keeps failing the same way.
- **F-28** Rips are not journaled for offline backfill.
  `Composition.CreateAppGraph` gives the rip view model the raw
  `engineVerify`, and `RipService` makes its own database contact, so an
  offline rip finishes with no verdict and nothing queued. Only Verify
  and Queue journal.
- **F-29** Optical drives are enumerated once, in the `RipViewModel`
  constructor, with no rescan. A drive attached after launch stays
  invisible until restart, and the page says "No optical drive found."
- **F-30** Diagnostic logs accumulate forever. One file per launch under
  `~/.config/CUETools2026/logs/`, with no pruning anywhere in
  `DiagnosticLog.cs`.
- **F-31** The glibc 2.38 floor is not only the .NET runtime. `objdump
  -T` on the 2026-08-15 publish shows the vendored `libmp3lame.so` and
  `MACLibDll.so` also reference `GLIBC_2.38`, so lowering the floor needs
  those two codec libraries rebuilt on an older toolchain as well. This
  bears directly on the packaging work in progress. Extended and partly
  corrected by F-38: the apphost reaches 2.38 on its own, through `fmod`
  and `fmodf`, so rebuilding only those two libraries would not have
  lowered the floor.
- **F-32** A fatal drive error can be unreadable. The drive-bar status
  uses `TextTrimming="CharacterEllipsis"` with no tooltip
  (`RipView.axaml:41-44`), and the stuck-drive guidance is about 450
  characters. The one message whose entire value is "power the drive off
  and on" is the most likely to be clipped.
- **F-33** The rip codec button shows only the format label, while
  Convert shows the format plus the implementation. On the page where the
  choice is frozen for the whole transaction, which encoder will run is
  visible only in a tooltip.
- **F-34** The rip track grid has no column headers
  (`RipView.axaml:145-167`), so four evidence columns render as
  unlabelled numbers, and `CrcEvidenceTip` is never bound on this head.
- **F-35** `SetPostRipRepair` sets `RepairLastRipText` for the "no
  unambiguous album input" case but leaves `CanRepairLastRip` false,
  and the button binds `IsVisible` to that flag, so the explanatory text
  never renders.
- **F-36** `CUESheet.LoadAndResizeAlbumArt` fetches a cover through
  `CTDB.FetchFile` with no host or scheme validation at all. It is
  currently reachable only from the classic head
  (`CUETools/frmCUETools.cs:971`), so it is a hazard to remember rather
  than a live defect on Linux.

## Suggested order for this pass

1. F-23 and F-24: the app reaches the network unasked, and one of those
   paths accepts any host. Both are promises the documentation currently
   makes and the code does not keep.
2. F-25 and F-26: Salvage silently degrades on two of three buttons, and
   its own archive log misdescribes it.
3. F-27 and F-28: two ways a user ends up with no database verdict and no
   queued retry.
4. F-31 before the next packaging attempt.
5. The rest as ordinary UX work.

## F-37 A verify writes a `.toc` beside every album it checks

Measured 2026-08-17. An album folder holding only `.flac` and `.cue` was
verified once, and the run wrote both `<name>.accurip` (2624 bytes) and
`<name>.toc` (558 bytes), the same human-readable track table a rip
writes. `Composition.cs` sets `config.advanced.CreateTOC = true` for the
whole profile, and `VerifyService.TrySetVerifyLogTarget` gives the sheet
an output path rooted at the source, so the engine's `CreateTOC` branch
fires on the verify path too.

Nothing is wrong with the file. The question is whether it should appear
at all: checking somebody else's album now leaves two new files in their
folder rather than one, and the second is a layout table they did not ask
for and cannot obviously act on. The report is the point of a verify; the
`.toc` is rip evidence.

Severity: low, and it is a decision rather than a defect. Options are to
scope `CreateTOC` to the rip and repair paths, to keep it and say so
plainly on the Verify page (which it now does), or to expose it as a
setting.

---

# Live evidence, 2026-08-17

## The stuck-drive state reproduced on a second drive model

The 2026-08-14 characterization was made on the ASUS BW-16D1HT in an OWC
enclosure. On 2026-08-17 the same state appeared on an HL-DT-ST BD-RE
WH16NS40, firmware 1.05, during a secure Test and Copy of disc 3 of a
3-CD set.

Sequence, from `rip-C.log`:

1. The first window was in trouble immediately: `stuck window at 0%
   errors=2027`, at the very start of the program rather than at a
   scratch mid-disc.
2. Deep recovery worked it down across passes (2330, then 2190, 2027,
   1923 fresh errors at 32x), and the slip probe reported
   `reads identical (cache or stable, not jittering)`.
3. The drive then began rejecting every read shape down to single
   sectors, in regions it had read successfully moments earlier:
   `payload_batch_fallbacks=127`, `pinpoint_retries=2048`,
   `corroborated_unreadable_pinpoints=2048`, `cache_defeat_retries=30`,
   all with `IllegalRequest / 24/00`.
4. `unresponsive-signature=yes`. The run failed closed after 341s with
   the shipped power-cycle guidance, `ok=False`, `readsUsed=0`, nothing
   published, and no verdict claimed.

Independently confirmed from outside the app: a `CDROMREADTOCHDR` ioctl
returned EIO on `/dev/sr2` while both other drives answered normally. The
owner power-cycled the enclosure, the device re-enumerated, and the same
ioctl then returned the full TOC (20 tracks, leadout 64:43).

What this adds to SLICE-011's evidence:

- The classifier is not tuned to one drive. A different manufacturer,
  model and firmware produced the same signature and the same verdict.
- The cure is confirmed a second time: a power cycle cleared it, and the
  guidance's insistence that a replug may not be enough is what the owner
  followed.
- It failed closed. No partial output, no verdict, `failedWindows=0`, and
  the completed evidence from the earlier disc was untouched.

## The disc, not only the drive

Worth separating for the manual's troubleshooting: this disc's trouble
began at 0% with over two thousand errors in the first window, which is
the start of the program rather than a scratch. Whether that is a second
bad master in this set or a drive that cannot read this particular disc
is not established. Reading the same disc in a different drive would
separate the two, and the Rip page's troubleshooting currently cannot
tell a reader how to make that distinction.

Still not established as of 2026-08-17, and F-40 records the same open
question about the same drive and disc. Note the wording above says "a
second bad master", which assumed an identification that later turned out
to be wrong: see the correction at the head of F-40.

## F-38 The 2.38 glibc floor is set by the build machine, not the code

Measured 2026-08-17 with `objdump -T` over every shipped native binary in
`src/CUETools.Linux.App/bin/Release/net10.0/linux-x64/publish`. Three of
the seven reach `GLIBC_2.38`, which matches the manual's "the app binary,
and two of the audio codec libraries" exactly:

| Binary | Highest GLIBC version needed |
| --- | --- |
| `CUETools.Linux.App` (AOT apphost) | 2.38 |
| `libmp3lame.so` | 2.38 |
| `MACLibDll.so` | 2.38 |
| `libFLAC_dynamic.so` | 2.34 |
| `wavpackdll.so` | 2.34 |
| `libSkiaSharp.so` | 2.27 |
| `libHarfBuzzSharp.so` | 2.14 |

The finding is what those symbols turn out to be. Not one is a glibc 2.38
feature. The apphost needs only `fmod` and `fmodf`, which 2.38 re-versioned;
the older `fmod@GLIBC_2.2.5` still exists on new systems, so a build linked
on an older one binds that instead. The two codec libraries need only
`__isoc23_strtol` and `__isoc23_wcstol`, which are what glibc 2.38's headers
redirect plain `strtol`/`wcstol` to under a C23-aware compiler (GCC 13+).
Those variants differ from the originals only in parsing `0b` binary
literals, which neither library does.

So rebuilding the release under an older glibc should lower the floor to
2.34 with no source change, and dropping the two codecs would not help,
because the apphost reaches 2.38 by itself. Inferred, not verified: no such
build has been attempted. The requirement stands as written until one is.

Manual updated: `pages/install.md` now says the floor is an accident of the
build machine rather than leaving "being worked on" unexplained, and
`notes/install.md` carries the table.

## F-39 The repair-headroom doc comment states the wrong conclusion

Found 2026-08-17 while settling needs-verification entry 13. The XML doc on
`CDRepairFix.WorstStripeErrors` (`CUETools.AccurateRip/CDRepair.cs`) read:

> at capacity, one more error in that stripe would have made the disc
> unrecoverable

That is the same claim the manual carried and it is wrong for the same
reason. `CUEToolsDB.LookupCTDB` (`CUETools.CTDB/CUEToolsDB.cs:474-490`)
walks a ladder of `npar = 4, 8, 16`, capped at the entry's own `Npar` and at
`AccurateRipVerify.maxNpar`, and stops at the first depth that recovers.
`CUETools.AccurateRip/CDRepair.cs:182` then sets `columnCapacity = npar / 2`
for whichever depth succeeded. So a fix reporting capacity 4 ran at
`npar = 8`, the second rung, with 16 never fetched. One more error there
would have defeated that rung and sent the lookup to the next one.

The disc is only out of headroom when the capacity comes from the last rung
the ladder can reach. Worth recording separately from the manual fix,
because the comment is where the manual's wording came from: correcting the
prose alone would have let it grow back.

Fixed in the fork. The comment now states the depth qualifier and warns
against describing a fix at capacity as one error from unrecoverable
without checking which rung it ran at.

## F-40 A severely damaged disc exercises the reread path without touching the fatal path

Measured 2026-08-17 on the PLDS drive (drive A). Verify-only, so nothing
was written.

Corrected the same day, and the correction matters. This entry first said
the disc was "the third disc of the owner's three-disc set, which the owner
identified in advance as likely a bad master". That was wrong. The owner
later gave the arrangement as Reggae Roots CD1 (KBOX3604A) in the PLDS, CD2
(KBOX3604B) in the ASUS, and CD3 (KBOX3604C) in the LG, so the disc ground
here was **CD1, not the suspected bad master**, which had not been read at
all at that point.

The mistake came from taking "the PLDS has the bad master" as an
identification when it was one of two conflicting statements, the other
being that disc 3 was the suspect. Nothing in the run's own evidence could
settle it: the log records `drive='PLDS     - DVD-RW DU8A5SH'` and
`chosen_release=False` and carries no TOC, title, or track count. Two
claims about which disc was where should have been recorded as a conflict
and resolved by measurement, not by picking the more recent one.

Every measurement below is unaffected, because they describe the disc that
was actually in the drive. Only the identity attribution was wrong.

The run was stopped deliberately after 17 minutes. What it showed by then:

| Window | Passes | Errors | Converged |
| --- | --- | --- | --- |
| 0 | 30 | 488 | no |
| 2400 | 30 | 552 | no |
| 4800 | 30 | 683 | no |

Three consecutive windows from sector 0, none converging, error counts
rising. That shape is a defective pressing rather than a scratch: the
2026-08-12 walkthrough disc had 129 damaged sectors across the whole disc
and its damage sat in the outer third, whereas this one had roughly 1,700
in the first 7,200 sectors, starting at the program's beginning.

Behaviour worth keeping, all measured:

- The engine gave up per window rather than per disc. Each window burned
  its 30 passes, logged `WARN gave up on window ... (unreadable by drive)`,
  and moved on. No crash, no abort, no fatal classification.
- The D11 stuck-drive classifier did not fire, correctly. No `24/00`, no
  `unresponsive-signature`. Bad media and a wedged drive are the
  distinction that policy turns on, and a genuinely unreadable disc did not
  trip it.
- The speed ladder was exercised: 16x, 12x and 8x requested, with recovery
  passes running at 0x and stepping back up on entering a fresh window.
- The drive was healthy after an abort mid-read: `CDROM_DRIVE_STATUS`
  returned `disc ok` and the only remaining holder of `/dev/sr0` was
  `gvfsd-cdda`.

Two limits on what this proves. Progress was 1% in 16 minutes, so the disc
was never carried to a verdict, and no AccurateRip or CTDB result exists
for it. And the log carries only the `rip.recovery`, `rip.reread` and
`rip.speed` channels, with no SCSI channel in this or any earlier rip log,
so the absence of sense data is not evidence that the drive reported none.
Whether these reads succeeded with unstable payloads or failed with sense
the log does not surface is unknown.

What the drive can still do is worth recording next to that. All three
drives returned a full TOC on demand afterwards, CD1 included: 20 tracks,
64:43, CDDB `350f2914` from the PLDS, against 69:14 / `2a103814` for CD2 in
the ASUS and 68:51 / `35102114` for CD3 in the LG. So the PLDS reads this
disc's lead-in without difficulty and fails only in the audio payload, and
the three discs are confirmed distinct rather than the same disc read three
times.

Whether CD1 is defective or the PLDS specifically cannot read it was left
open here. It is now nearly closed, from the other end.

CD3, the disc actually suspected of being a bad master, was verified in the
LG later the same day and is the worst of the three: errors climbing to
1,576 per window, 6% in 55 minutes. That confirms the owner's advance call
and shows this failure shape is not particular to the PLDS.

Then the PLDS read a different, known-good disc end to end to an accurate
verdict at AccurateRip 226 of 262 (F-45). A drive that does that is
working, so the remaining explanation for CD1 is the disc.

Short of proof, and worth keeping short of it: only reading CD1 itself in a
second drive proves it, and that still needs a physical swap. What has
changed is that the drive-fault hypothesis now requires the PLDS to fail on
one disc while succeeding on another, which is what a bad disc looks like.

This disc cannot settle needs-verification entry 13. Damage at this density
is far beyond what CTDB parity repairs, so no repair would ever run on it.

## F-41 The startup stopwatch reported 0 ms on every run

Measured 2026-08-17. `--smoke` prints `startup-to-window-ms=` from
`Program.Startup.ElapsedMilliseconds` at `window.Opened`, and the comment
beside it says the stopwatch "starts in Main so the number covers the
whole launch". It printed exactly `0` on every run of the shipped AOT
build, three for three.

The cause is C# static initialization. `Program` declares

```csharp
internal static readonly Stopwatch Startup = Stopwatch.StartNew();
```

and has no static constructor, so the compiler marks the type
`beforefieldinit` and the runtime may defer that initializer until the
first access to one of the type's static fields. `Main` being a method on
the same type does not force it. The first field access was the
`ElapsedMilliseconds` read itself, so the stopwatch was created and read in
the same instant.

Fixed by starting it explicitly as the first statement of `Main` and
leaving the field as a plain `new Stopwatch()`, which does not depend on
when the initializer runs.

Measured before and after, three runs each:

| Build | Reported | Wall clock |
| --- | --- | --- |
| AOT, before the fix | 0 ms every run | 699, 699, 797 ms |
| JIT via `dotnet`, after the fix | 3012, 3274, 3139 ms | 3077, 3354, 3261 ms |
| AOT, after the fix | 688, 709, 635 ms | 727, 750, 688 ms |

After the fix the reported number tracks wall clock closely: within about
40 ms on the AOT build and 80 ms on the JIT build, which is the process
teardown that follows `Opened`. The AOT figures also agree with the
external wall-clock timings taken before the fix, so the instrument is now
measuring the thing its name claims.

The manual is not at fault here, and it is worth being exact about that.
`pages/install.md` documents the line's format and states no number, so
there was never a speed claim to disprove. What it did document was an
output that always read zero, which would leave a reader checking their
install with a number that could not mean anything.

Now that the instrument works, the measured figures are worth having.
The shipped AOT build reaches a visible window in roughly 0.68 seconds. A
JIT build run through `dotnet` takes about 3.1 seconds, so any startup
figure has to stay attached to the packaged app rather than to running
from source. Both are warm-cache on this workstation, and a first cold
launch will be slower.

Worth noting for its own sake: a smoke test whose only output was a
hardcoded-looking `0` passed CI unremarked. The number was never asserted
against a bound, so nothing failed when the instrument stopped measuring.

## F-42 The AppImage does not need libfuse2

Tested 2026-08-17 by running the shipped artifact,
`bin/packages/CUEToolsLinux-0.1.0-alpha-x86_64.AppImage`, on this
workstation. `notes/install.md` said the AppImage "needs FUSE, provided by
the `libfuse2` or `libfuse2t64` package depending on your distribution".
Neither is installed here: `libfuse2` has no candidate at all on this
release, and `libfuse2t64` is available but not installed. The AppImage ran
regardless, reaching a window and exiting cleanly.

It really did mount rather than quietly falling back, which is the part
worth proving, because an extract-and-run fallback would look identical
from outside. `--appimage-mount` reported `/tmp/.mount_CUETooihBKKM`, and
`mount` listed it as `type fuse.CUEToolsLinux-0.1.0-alpha-x86_64.AppImage`
with `ro,nosuid,nodev,user_id=1000`.

The requirement is a `fusermount` binary on the PATH, not a specific
library package. On this system `fuse3` provides it:
`/usr/bin/fusermount` is a symlink to the setuid `/usr/bin/fusermount3`.
Naming `libfuse2` would send a reader to install a package they do not
need, and on Ubuntu 24.04 to one that does not exist under that name.

`pages/install.md` was already correct, and says the AppImage "needs a
`fusermount` program on your `PATH`" with `--appimage-extract-and-run` as
the fallback. Only the notes over-specified it, and they now carry the
measurement.

Separately, this run confirmed F-41 in the released artifact: the AppImage
built 2026-08-14 printed `startup-to-window-ms=0`, so the broken stopwatch
shipped rather than being a working-tree regression.

## F-43 An unrecognised launch flag is silently ignored

Found 2026-08-17 by losing 45 minutes to it. A verify-only run was started
against the Release publish with `--rip-verify-cli C`. The app printed
`startup-to-window-ms=656`, opened its window, and sat there until the
timeout killed it 45 minutes later. Nothing said the flag had not been
understood.

The flag is real but Debug-only. `CUETools.Linux.App.csproj:22-33` defines
`RIP_DIAGNOSTIC` and references `CUETools.Ripper.SCSI` only under
`Configuration == Debug`, which is deliberate (D-053: the rip transport
diagnostic is a dev-only surface and Release publishes are byte-unaffected).
So `--rip-diagnostic`, `--rip-verify-cli` and `--rip-tc` exist in a Debug
build and simply do not exist in a Release one.

The defect is not that they are compiled out. It is that arguments are
handled by a chain of `args.Contains(...)` tests in
`App.axaml.cs:95-260` with no central parser and no final check for
anything left over, so *any* argument the app does not recognise produces a
normal silent launch. That covers a mistyped documented flag as much as a
Debug-only one: `--quue` instead of `--queue`, or `--convert-to` without
`--convert`, each opens the window on the Verify page as though nothing had
been asked for.

`pages/install.md` documents nine launch forms. A reader who mistypes one
gets no signal that they did.

Severity: low for a user, who sees a window open and can retry. Higher for
automation and for evidence runs, where a silently ignored flag produces a
process that looks alive and yields nothing, and where the operator may
conclude the hardware or the disc was at fault. That is exactly what
happened here: the first reading of the empty log was that the LG drive had
wedged.

Proposed fix, not implemented: collect the recognised arguments while
parsing, and on any unconsumed argument write one line to stderr naming it
and exit non-zero rather than opening a window. A Release build should say
that a `RIP_DIAGNOSTIC` flag needs a Debug build, rather than ignoring it,
because the flag name is knowable at compile time even when its
implementation is not.

## F-44 A stuck window does not mean a damaged rip

Measured 2026-08-17 on the ASUS drive (drive B) with Reggae Roots CD2
(KBOX3604B), verify-only. The first clean end-to-end result of the day, and
the most useful one, because it is the control the other two discs lacked.

The verdict: **AccurateRip accurate, 4 of 4. CTDB verified, confidence 4 of
7.** Elapsed 741 seconds.

The disc was not pristine. Three windows exhausted their rereads and were
declared stuck:

| Position | Unresolved sectors |
| --- | --- |
| 14% | 2 |
| 15% | 2 |
| 43% | 1 |

And the rip was still bit-exact. That deduction is forced rather than
guessed: AccurateRip compares an exact CRC over the audio program, all
three positions sit inside it, and the result was `accurate=True` at 4 of 4.
Had any of those five sectors carried wrong samples, the CRC could not have
matched.

So `stuck window` is a statement about read *stability*, not about
correctness. The reread layer gives up when repeated passes stop agreeing,
and this run shows that a sector whose passes never converged can still
have been read correctly every time. Why the vote did not settle is not
established here.

That matters for what the manual tells a user. A log carrying "gave up on
window ... (unreadable by drive)" reads like a ruined rip, and on this disc
it accompanied a perfect one. The Rip page's troubleshooting should not
equate the two.

Every hardware-anomaly counter was zero: `control_transition_retries`,
`read_communication_retries`, `cache_defeat_retries`,
`cache_defeat_chunk_fallbacks`, `cache_defeat_wake_readiness_retries`,
`payload_batch_fallbacks`, `pinpoint_retries`,
`corroborated_unreadable_pinpoints`, `drive_reported_timeout_pinpoints`,
`drive_reported_timeout_batches`. `c2_mode=3`, cache defeat flushing
786,432 bytes per secure reread.

### What this settles about the other two discs

It is the control that kills the systematic-bug hypothesis. When CD1 and
CD3 both stalled at window 0 in different drives, a structural fault in the
read path looked plausible. CD2 read straight through the same code on a
third drive and verified accurate, so the machinery is sound and the two
bad discs are bad discs.

The set, measured the same day, same code, one disc per drive:

| Disc | Drive | Worst window | Outcome |
| --- | --- | --- | --- |
| CD1 (KBOX3604A) | PLDS | 683 errors, minFresh 236 | 1% in 16 min, abandoned |
| CD2 (KBOX3604B) | ASUS | 2 errors | **accurate, AR 4/4, CTDB 4/7** |
| CD3 (KBOX3604C) | LG | 1,475 errors and climbing | 4% in ~50 min, abandoned |

The owner's advance call that disc 3 was likely a bad master is confirmed:
CD3 is the worst of the three. CD1 being nearly as bad was not expected by
anyone and remains the surprise of the set.

Still not established: whether CD1 is defective or the PLDS specifically
cannot read it. CD3 failing the same way in a different drive makes a
PLDS-specific fault unlikely, but only reading CD1 in a second drive
settles it, and that needs a physical swap. See F-40.

## F-45 Three known-good discs, three drives, three accurate verdicts

Measured 2026-08-17 at the owner's request, ahead of any CTDB submission:
one known-good disc per drive, verify-only, run concurrently.

| Drive | Tracks | Elapsed | AccurateRip | CTDB | Verdict | Stuck windows |
| --- | --- | --- | --- | --- | --- | --- |
| A, PLDS DU8A5SH | 4 | 791 s | 226/262 | 602/660 | accurate | 1 |
| B, ASUS BW-16D1HT | 20 | 413 s | 6/6 | 12/13 | accurate | 0 |
| C, LG WH16NS40 | 8 | 350 s | 245/388 | 799/835 | accurate | 0 |

Three things follow, and each closes something that was open.

### The PLDS is not a broken drive

This is the one that matters most. F-40 left open whether Reggae Roots CD1
was defective or the PLDS specifically could not read it, and the honest
answer was that nothing had separated them. The PLDS has now read a
different disc end to end to an accurate verdict at AccurateRip 226 of 262.
A drive that does that is working.

So the remaining explanation for CD1 is the disc. That is not the same as
proof: only reading CD1 itself in a second drive proves it, and that still
needs a physical swap. But the drive-fault hypothesis now requires the PLDS
to fail on exactly one disc while succeeding on another, which is what a bad
disc looks like.

### F-44 holds on a second disc, in a second drive

Drive A's disc had one stuck window, at 88% with a single unresolved sector,
and verified accurate. That is an independent repeat of what CD2 showed on
the ASUS: a window the reread layer gave up on, in a rip AccurateRip then
confirmed bit-exact.

Two discs, two drives, same result. A stuck window is a statement about read
stability and not about the audio, and the manual can now say so on the
strength of more than one disc.

### The 3E/02 carve-out fired live and recovered

Drive A's run recorded `drive_reported_timeout_batches=1`. That is the
corroboration-gated HardwareError 3E/02 (TIMEOUT ON LOGICAL UNIT) path: a
multi-sector batch where the drive surrendered, decomposed into independent
single-sector reads rather than being treated as fatal.

It happened during a run that finished accurate. The carve-out exists
precisely so a drive's own surrender on one batch does not end a rip that is
otherwise fine, and this is the first record of it doing that on a good disc
with a clean verdict at the end. Every other anomaly counter was zero:
`read_communication_retries`, `payload_batch_fallbacks`, `pinpoint_retries`.

### For the submission evidence run

Drive C's disc is the strongest candidate for S12-002. Its pressing already
carries 835 CTDB submissions, so a new one adds confidence to a
well-established entry rather than creating a fresh one that nothing else
can corroborate.
