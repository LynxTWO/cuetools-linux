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
  bears directly on the packaging work in progress.
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
