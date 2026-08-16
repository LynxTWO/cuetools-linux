# Repair a damaged album

Repair rebuilds damaged audio from recovery data stored in the CUETools
Database ([CTDB](glossary.md#ctdb-cuetools-database)). Of the two
databases a verification checks (AccurateRip and CTDB), CTDB is the one
that keeps [parity](glossary.md#parity), so it can locate the damage in a
rip and rebuild it. Repair is offered after a verification finds damage
CTDB has parity for, and it can rescue a rip from a scratched disc
without ripping the disc again. Repair does not change your original
files. It reads them, and writes the repaired album into a new folder
next to them.

## Before you start

| Item | What to know |
| --- | --- |
| Input | One disc whose card reads `REPAIRABLE` after a verification. Repair runs from that card, one disc at a time. |
| Result | A new folder beside the source files, named after the CUE sheet, playlist, or audio file you verified: `album.cue` gives `album - repaired`. It holds the repaired audio, a CUE sheet, an AccurateRip report, a repair report, and a machine-readable receipt. |
| Original files | Read only. CUETools records the size and SHA-256 fingerprint of every source file before the repair starts, and checks them again before the repaired folder is published. |
| Output format | FLAC, whatever the source format was. Each file keeps its source name with a `.flac` extension, so `track01.wav` becomes `track01.flac`. |
| Network | Required, for the whole run. Repair downloads recovery data from CTDB, then asks both databases (AccurateRip and CTDB) about the repaired copy. |

A repair has no offline mode. An offline verification is queued and
retried later (see
[offline behavior and backfill](offline-and-backfill.md)), but a repair
needs both databases while it runs, so run it when you have a
connection.

WAV, FLAC, and Apple Lossless (`.m4a`) decode in every build. Support for
other formats depends on the codecs included with your build, and the
[codecs page](codecs.md) lists what this build reads and writes.

## Repair a disc

1. Verify the album first. Open the **Verify & Repair** page, load the
   album, and press **Verify album**. See
   [verify an album](verify.md).

2. Find a disc card with the amber `REPAIRABLE` chip. Its CUETOOLS DB
   panel reads `damage found | parity available`, and a CTDB PARITY
   REPAIR panel appears on the card showing how much damage was found.

3. Press **Repair this disc**. The button is on the repairable card
   only, under the chip.

4. Read the confirmation window. Its title is "Create repaired copy from
   CTDB parity", and it says:

   ```text
   Repair will build a new sibling folder for <disc>, independently
   verify the repaired audio, and publish it only if verification
   succeeds. The selected source files will not be changed.

   Proceed with repair?
   ```

   `<disc>` is the name of the disc you picked. Press **OK** to start,
   or **Cancel**. Cancelling runs nothing, and the card stays exactly as
   it was, so you can press **Repair this disc** again later.

5. Wait for the run to finish. The album verdict changes to `Repair in
   progress`, the disc chip changes to `WORKING`, and the status line at
   the bottom of the page reads `Repairing <disc> from CTDB parity...`.

6. On a multi-disc set, repeat from step 2 for each repairable disc.
   Each disc is repaired on its own, and gets its own repaired folder.

## What happens next

Repair works in a hidden staging folder next to your source files, named
`.cuetools-repair-<random>.staging`. Everything is built there first, and
the folder is renamed to its final `- repaired` name in one step at the
very end. So the repaired album appears complete or not at all, and a run
that fails takes its staging folder with it.

The status line follows the run through these stages:

- "Building an isolated repaired copy..." while CTDB's recovery data is
  applied and the repaired FLAC files are written.
- The engine's own progress text as each stage runs, for example
  "Generating AccurateRip report...".
- "Independently verifying the repaired copy..." while the repaired
  files are decoded again from scratch and put to both databases.

The progress bar at the bottom of the page tracks one stage at a time,
not the run as a whole: it fills and restarts as the run moves from the
CTDB lookup to the repaired encode and then to the independent
verification. On the
card, the CTDB PARITY REPAIR panel switches to a live view: the headline
reads "Reconstructing damaged sectors from parity...", the pill on the
right reads `REPAIRING`, and a green sweep crosses the damage strip in
step with the run's progress, turning the marks it passes green.

**Stop after disc** stays on screen while a repair runs, because the page
is busy. It does not cancel a repair. A repair that has started runs to
its own end, and either publishes or fails.

## Read the result

The album verdict at the top of the page reports the whole run.

| Album verdict | What it means |
| --- | --- |
| `Repair in progress` | A repair is running now. |
| `Repaired copy verified` (or `2 repaired copies verified`) | Every repaired copy was built and [independently verified](glossary.md#independently-verified), and no disc is still repairable or failed. |
| `1 disc can be repaired from CTDB` | A disc is repairable and has not been repaired yet. A repair that ran and failed also leaves this verdict, because the damage and the parity are both still there. |

The one-line tally underneath counts each state: `1 repaired`,
`1 repairable`, or both when a set is part way through.

The disc card's chip reports that disc.

| Chip | Color | What it means | What to do next |
| --- | --- | --- | --- |
| `REPAIRABLE` | amber | CTDB found damage it has recovery data for. | Press **Repair this disc**. |
| `WORKING` | amber | This disc is being repaired now. | Wait. |
| `REPAIRED + VERIFIED` | green | A repaired copy was built and [independently verified](glossary.md#independently-verified). | Use the repaired copy. The folder it was saved to is shown in green at the bottom of the CTDB parity repair panel. |

After a repair publishes, the card's ACCURATERIP and CUETOOLS DB panels
describe the repaired copy rather than the source, because they are
filled in from the fresh verification of the repaired files. The track
evidence table changes with them. Reading those panels works exactly as
it does after a verification; see
[read the result](verify.md#read-the-result).

### The CTDB parity repair panel

This panel appears on a card as soon as a verification finds damage CTDB
can rebuild, and it stays after a repair.

| Panel text | What it means |
| --- | --- |
| `7,150 samples in 129 sectors` | The size of the damage: how many 16-bit audio values CTDB can rebuild, and how many CD sectors they fall in. |
| `worst stripe 4/4` | Parity headroom. The most heavily damaged [parity stripe](glossary.md#parity-stripe) needed 4 corrections, and 4 were available to it. |
| `parity headroom not reported` | The stripe figures were not available for this entry, so only the damage size is shown. |
| A list like `41:29:57,41:32:68,57:28:38` on the right | Where the damage is, as positions on the disc in minutes:seconds:frames. The full list is in the tooltip. |

Under those numbers is the damage strip. It draws the disc from the
inside (left) to the outside (right) from the real per-sector damage map,
so a scratch shows up as a cluster of marks at the place on the disc
where it is. Damaged sectors are amber to red before a repair, and green
once they have been rebuilt. Its headline and pill track the state:

| Headline | Pill | When |
| --- | --- | --- |
| `<samples> samples across <sectors> sectors - recoverable from parity` | `REPAIRABLE` | After a verification found repairable damage. |
| `Reconstructing damaged sectors from parity...` | `REPAIRING` | While the repair runs. |
| `Recovered 7,150 samples across 129 sectors` | `REPAIRED` | After the repaired copy was published. |

The line under the headline names the math (Reed-Solomon), the parity
depth the database holds for this disc (for example `npar=16 parity
symbols / 10-sector stride`), and the arithmetic field it works in
(`GF(2^16)`).

The five chips along the bottom are the stages of a
[Reed-Solomon](glossary.md#reed-solomon) decode: `syndrome`, `locate`,
`Chien`, `Forney`, and `apply`. The first four light up as soon as a disc
is repairable, because that math runs during verification. `apply` lights
up only after a repaired copy has been written and published.

![Three copies of the CTDB parity repair strip, stacked. The first has an amber headline "18,342 samples across 12 sectors - recoverable from parity" with a REPAIRABLE pill, red and amber marks on the strip, and the syndrome, locate, Chien and Forney chips lit while apply is dim. The second reads "Reconstructing damaged sectors from parity..." with a REPAIRING pill, the marks left of a sweep line now green and those to the right still red. The third reads "Recovered 18,342 samples across 12 sectors" in green with a REPAIRED pill, every mark green, and the apply chip lit. All three show the line "Reed-Solomon . npar=8 parity symbols / 10-sector stride . GF(2^16)".](2026-08-12-repairscope-states.png)

*The panel's three states, drawn with the same example figures so that
what changes between them is the state, not the disc.*

![The Verify and Repair page mid-repair. The album verdict reads "Repair in progress" with the tally "1 repairable", and the tiles beside it read 1 disc, 24 tracks, 1:08:22. The disc card's chip reads WORKING with a greyed-out Repair this disc button under it, the ACCURATERIP panel reads "no match | 0/82", and the CUETOOLS DB panel reads "damage found | parity available". The CTDB PARITY REPAIR panel reads "7,150 samples in 129 sectors" and "worst stripe 4/4", with the corrected ranges "41:29:57,41:32:68,57:28:38,58:04:47,58:05:13,58:05:34,59:14:17,59:..." along the top right. Its headline reads "Reconstructing damaged sectors from parity..." beside a REPAIRING pill, and the damage strip shows amber marks bunched toward the right, outer end of the disc. The syndrome, locate, Chien and Forney chips are lit; apply is dim. The status line reads "Generating AccurateRip report...".](2026-08-12-repair-real-disc-repairing.png)

*A real scratched disc, part way through its repair. The damage marks sit
where the scratch is, in the outer part of the disc.*

## What the repaired copy contains

The published folder is named for the file you verified, so `album.cue`
gives `album - repaired`. If a folder of that name already exists, the
next free name is used (`album - repaired (2)`), and the existing folder
is left alone.

| File | What it is |
| --- | --- |
| The audio files | The repaired tracks, as FLAC, under the source file names. A single-file source stays a single file; a per-track source stays one file per track. |
| `album.cue` | A CUE sheet written for the repaired files. |
| `album.accurip` | An AccurateRip report for the repaired audio, written from the independent verification. It is about the repaired copy, not the source. |
| `album - CTDB Repair.log` | The readable repair report: corrected samples and sectors, parity headroom, corrected ranges, both database verdicts, and the count of source files whose SHA-256 was unchanged. |
| `repair.verify` | The machine-readable receipt (`CUETOOLS_REPAIR_RECEIPT_V1`), in JSON: the repair figures, both confidences, and the size and SHA-256 of every source file and of every repaired audio file plus the CUE sheet. The reports are named in the receipt rather than hashed by it. |
| `.cuetools-complete` | Written last, after everything else. Its presence marks a folder that was published complete. |

The `album` part of those names comes from the source file's name, so a
disc verified from `disc1.cue` produces `disc1.accurip` and
`disc1 - CTDB Repair.log`.

Your titles, artists, any custom tags you had added, and embedded cover
art are carried into the repaired files. One family of tags is
deliberately not carried over: the source's stored database proofs. Every
tag whose name contains `ACCURATERIP` (`ACCURATERIPCRC`,
`ACCURATERIPID`, `ACCURATERIPCOUNT` and the rest), plus
`CTDBDISCCONFIDENCE` and `CTDBTRACKCONFIDENCE`, is dropped, because they
described audio that has now changed. The repaired copy's own proof is the report and receipt
written beside it.

Apart from its staging folder while it runs, and the finished folder at
the end, repair writes nothing into the folder your album is in. The
`.accurip` and `.toc` files your verification left next to the source
stay as they are, and no `.toc` file is written into the repaired folder.

![The same page after the repair finished. The album verdict reads "Repaired copy verified" with the tally "1 repaired". The disc card's ACCURATERIP panel reads "accurate | confidence 29", the CUETOOLS DB panel reads "verified | confidence 207", and the layout tile reads 24 tracks, 1:08:22. The CTDB PARITY REPAIR panel still reads "7,150 samples in 129 sectors" and "worst stripe 4/4", its headline now reads "Recovered 7,150 samples across 129 sectors" in green, and the damage marks on the strip are green. The status line reads "Repaired copy independently verified with durable evidence and saved to /tmp/claude-1000/-home-daniel-boyd-DEV-apps-cuetools-2026/fa658869-a3c1-4cec-b325-31cc...".](2026-08-12-repair-real-disc-verified.png)

*The same disc after its repair. Both databases now confirm the repaired
copy, and the status line names the folder it was saved to.*

## If something goes wrong

### The repair failed and the card still reads REPAIRABLE

The card's status line reads `Repair failed: <reason> The completed
verification evidence was kept.` Nothing was published, the staging
folder was removed, and your source files are as they were. The
disc stays repairable because the damage and the parity are both still
there, so you can try again once you have dealt with the reason.

These are the reasons you are most likely to see.

- "CTDB did not apply a repair. The source may already match or no
  recoverable entry was selected."

  The repair run makes its own fresh CTDB lookup, and that lookup did not
  end with a correction to apply. A failed or incomplete database contact
  arrives here too. Check your connection and verify the album again,
  then read the CUETOOLS DB panel. `lookup failed` means CTDB never
  answered, so try again once you have a connection. Any other reading
  that is no longer `damage found | parity available` means CTDB answered
  differently than it did before.

- "The repaired copy could not be independently verified against
  AccurateRip or CTDB."

  The repaired files were built and decoded again, and neither database
  returned a [confidence](glossary.md#confidence) above zero for them, so
  nothing was published. A lookup that did not complete reads the same
  way here, because it also produces no confidence, so check your
  connection before trying again.

- "Source filenames collide after conversion to repaired FLAC names."

  Two of your source files have the same name with different extensions
  (`track01.wav` and `track01.flac`, say), so both would become
  `track01.flac` in the repaired folder. This is caught before anything
  is written. Rename one of the source files, then verify and repair
  again.

- "A repair source changed while the repaired copy was being built."

  A source file was modified, replaced, or removed while the repair was
  running. Repair stops rather than publishing a copy built from files
  that no longer match what it started with. Let anything else that was
  writing to the album finish, then verify and repair again.

### There is no Repair this disc button on the card

The button appears only on a disc CTDB can repair: a card whose chip
reads `REPAIRABLE`, or `WORKING` with the button greyed out while that
disc's repair runs. That needs CTDB to hold recovery data for the exact
damage in your rip. A card
reading `NOT DATABASE-CONFIRMED` has no repair path, even when CTDB
returned entries for the disc layout: see the CTDB panel rows in
[read the result](verify.md#read-the-result) for what that panel is
telling you.

A card that already reads `REPAIRED + VERIFIED` has no button either.
Repairing a disc twice in one run is not offered; verify the repaired
copy on its own if you want to look at it again.

### A `.cuetools-repair-...staging` folder is left next to your album

A repair that ends normally, including one that fails, removes its own
staging folder. One left behind means the app could not remove it: it
was killed, the machine lost power, or the delete itself failed and was
recorded in the diagnostic log. Nothing looks for it again, since every
repair creates its own, so you can delete it.

### The Queue says an album is Repairable but does not repair it

The Queue page reports verify results using the Verify page's own words,
and `Repairable` is one of them. Repairing is a per-disc action with its
own confirmation, so it happens on the Verify & Repair page. Load that
album there and follow the steps above.

## Command line

```console
cuetools-linux --repair /path/to/album
```

`/path/to/album` is an album folder, CUE sheet, playlist, or supported
lossless audio file, exactly as on the Verify & Repair page. The flag
loads the album, starts the verification, and then repairs every
repairable disc it finds, in order.

`--repair` is your consent given up front, so the confirmation window
does not appear. Each disc gets one attempt: a disc whose repair failed
stays repairable and is not retried in that run.

## How it works

CTDB stores checksums plus [Reed-Solomon](glossary.md#reed-solomon)
recovery data for the discs it knows. When your audio almost matches an
entry, that recovery data lets CTDB work out which values are wrong and
what they should have been, which is why a repairable disc reports its
damage down to the sample and sector. The parity is fetched in increasing
depth, and CTDB stops at the first depth that can rebuild your disc. The
`npar=` figure names the depth the database entry advertises, so it can
be deeper than the one this repair actually settled on and used for its
`worst stripe` figure.

Repair is built as a transaction, and each step has to succeed before the
next one starts:

1. Every source file is hashed (SHA-256) before anything is read for
   conversion.
2. The engine's repair path applies CTDB's corrections and writes the
   repaired FLAC files, the CUE sheet, and the tags into the staging
   folder. The staging folder is checked to make sure every expected file
   was produced and none of them is empty, and that none of the generated
   paths points outside it.
3. The staged files are decoded again from scratch, as a fresh
   verification, and put to AccurateRip and CTDB. A confidence above zero
   from either database is what counts as verified. Without it the run
   stops here.
4. The evidence is written: the AccurateRip report, then the receipt,
   then the readable repair report, and `.cuetools-complete` last.
5. Everything is re-checked, source hashes included, and the staging
   folder is renamed to its final name.

That order is why a failed repair leaves nothing behind but your original
files, and why the repaired folder is either complete or absent.

When CTDB offers more than one recoverable version of a disc, the one
with the highest confidence is used. The order the server lists them in
is not a ranking, and repairing toward a low-confidence entry would
rewrite the rip toward the wrong [pressing](glossary.md#pressing).

The repaired copy is always written as FLAC, with no format choice.
Encoding to FLAC does not change the audio, and both databases compare
decoded audio, so the format the repaired copy is stored in has no effect
on its verdict.

## Case study: a scratched CD, 2026-08-12

This is the evidence run that the repair flow was accepted on, recorded
in `docs/SLICE-002-repair.md`. The album was a scratched 24-track CD
(1:08:22), ripped on Linux and then repaired with the `--repair` flag.
The two page screenshots above are from this run. The three-state strip
above them is a separate example render, drawn with its own figures.

Before the repair, AccurateRip reported `no match | 0/82`: it knew the
disc, and none of its submissions matched the damaged rip. CTDB reported
`damage found | parity available`. The measured damage was **7,150
samples in 129 sectors**, with the worst parity stripe using **4 of 4**
corrections, and the affected positions bunched in the outer part of the
disc.

After the repair, the published copy verified as AccurateRip
`accurate | confidence 29` and CTDB `verified | confidence 207`. Its
repair report recorded those as `29 / 82` and `207 / 235`. The rip that
matched nothing now matches other people's clean rips exactly. All 25
source files (24 WAV tracks and the CUE sheet) re-hashed SHA-256
identical to the snapshot taken before the first attempt.

The same session also tested the failure path, without meaning to. An
evidence-sealing bug (since fixed) made the transaction fail 18 times in
a row, and the process was then killed part way through a run. Afterwards
the source files still re-hashed byte for byte identical, no partial
repaired folder had been published, and each failed attempt had removed
its own staging folder.

## Related topics

- [How to verify an album, and how to read the verdicts](verify.md)
- [What happens when you verify offline, and how backfill catches up](offline-and-backfill.md)
- [Which audio formats this build reads and writes](codecs.md)
- [Terms used in this manual](glossary.md)
