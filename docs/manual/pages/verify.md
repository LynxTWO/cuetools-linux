# Verify an album

Verify checks an album you already have on disk against AccurateRip and
the CUETools Database (CTDB), two community databases of CD rip
checksums. A match means your audio is identical, sample for sample, to
rips other people made of the same disc. Verify reads your audio files
and does not change them. It writes two new files next to the album: a
dated report, and a `.toc` file listing the disc layout.

## Before you start

| Item | What to know |
| --- | --- |
| Input | An album folder, a CUE sheet (`.cue`), an `.m3u` playlist, or a lossless audio file. Multi-disc albums work when each disc has its own CUE sheet or playlist. |
| Result | An on-screen verdict for each disc, plus a `.accurip` report and a `.toc` file next to each disc's files. |
| Original files | Read only. Verify does not change your audio files or their tags. |
| Network | The database checks contact AccurateRip and CTDB. Without a connection the run still completes; see [What happens next](#what-happens-next). |

WAV, FLAC, and Apple Lossless (`.m4a`) decode in every build. Support for
other formats depends on the codecs included with your build, and the
[codecs page](codecs.md) lists what this build reads and writes.

## Run a verification

1. Open the **Verify & Repair** page.

2. Load the album in any of these ways:
   - Drag an album folder, CUE sheet, playlist, or audio file anywhere
     onto the page.
   - Press **File...** and pick the files. The picker allows more than
     one file, so you can select a CUE sheet per disc of a multi-disc
     set.
   - Press **Folder...** and pick the album folder. CUETools finds the
     CUE sheets inside it.

3. Check the status line at the bottom of the page. A loaded album reads
   "Ready to verify one disc." or, for a multi-disc set, "Ready to
   verify 2 discs in order." Each disc gets its own card.

   If the selection is ambiguous, nothing loads, and the status line
   says what to change. For example, when two CUE sheets describe the
   same audio files: "Two manifests reference the same audio (album.cue
   and copy.cue). Choose the intended CUE/playlist so CUETools does not
   have to guess."

4. Press **Verify album**.

5. On a multi-disc set you can press **Stop after disc** at any time.
   The disc in progress finishes, and its result is kept; the remaining
   discs are not verified. The status line confirms: "Stopped after 1 of
   2 discs. Completed results were kept."

## What happens next

Discs verify one at a time, in the album's disc order. The status line
names the current disc ("Verifying disc 1 of 2: ..."), the working
disc's card shows `WORKING`, and discs that have not run yet show
`PENDING`. A progress bar runs along the bottom of the page.

As each disc finishes, its card fills in with the verdict, both database
panels, and the track evidence table, and its report file is written
next to that disc's files.

Without a network connection the run still completes: every file is
decoded, and every checksum is computed. While a disc runs, the status
line shows "Offline: database verification queued for automatic
backfill.", and a later status message can replace it, so watch for it
as each disc finishes rather than at the end. The database comparison
runs again automatically on a later launch. See
[offline behavior and backfill](offline-and-backfill.md).

## Read the result

The album card at the top gives one headline for the whole run, with
disc, track, and duration counts beside it and a one-line tally
underneath (for example "1 database-confirmed | 1 failed").

Before and during a run the headline just reports where things stand:
`Ready to verify` or `2-disc set ready` before you start, then `Album
verification in progress`, or `Repair in progress` while a repair runs.

Once the run finishes, the headline reports the most pressing thing that
happened, in the order below. A disc needing attention outranks the ones
that went well, so an album whose discs AccurateRip confirmed still
leads with a repairable disc if it has one.

| Album headline | What it means |
| --- | --- |
| `1 disc could not be verified` | At least one disc failed; you can read what the error was on the card. |
| `1 disc can be repaired from CTDB` | CTDB found damage it holds recovery data for. See [repair](repair.md). |
| `Repaired copy verified` | A repaired copy was built and [independently verified](glossary.md#independently-verified). |
| `Album verified` (or `All 2 discs verified`) | Every disc completed, a database confirmed each one, and nothing failed, needed repair, or was repaired. |
| `Verification complete - review the evidence` | The run finished with a mix of outcomes not covered above, for example a disc no database could confirm, or a run you ended with **Stop after disc**. Be sure to read each disc card to see exactly what happened. |

Each disc card carries an outcome chip in its top-right corner:

| Chip | Color | What it means | What to do next |
| --- | --- | --- | --- |
| `DATABASE VERIFIED` | green | At least one database confirmed this disc's audio. | Nothing; the rip matches other people's rips, so you're all set. |
| `NOT DATABASE-CONFIRMED` | neutral | The disc was read and checksummed, but neither database confirmed it. That covers three situations: no database knows this [pressing](glossary.md#pressing), a database holds rips for the layout but none match yours, or a lookup did not complete. | Read both panels and the message line under the track table, which says which one happened. |
| `REPAIRABLE` | amber | CTDB found damage it can repair. A repair that ran and failed leaves this chip unchanged, because the damage and the parity are both still there. | Press **Repair this disc**; see [repair](repair.md). If a repair already failed, read the card's status line before pressing it again. |
| `REPAIRED + VERIFIED` | green | A repaired copy was built and [independently verified](glossary.md#independently-verified). The card shows where it was saved. | Use the repaired copy. |
| `FAILED` | red | The disc could not be verified; the card's status line shows the error. | Fix what the message names (for example a track file that was moved or renamed after you loaded the album, or a file this build has no codec for), then verify again. |
| `WORKING` | - | This disc is verifying now. | Wait. |
| `PENDING` | - | This disc has not been verified in this run. | Press **Verify album**, or wait for an earlier disc to finish. |

The ACCURATERIP panel reports the comparison with other people's rips:

| Panel text | What it means |
| --- | --- |
| `accurate \| confidence 4` | Every track matched other people's rips, and the least-matched track agreed with 4 of them. The album number describes the whole disc by reporting its weakest track's [confidence](glossary.md#confidence). |
| `no match \| 0/82` | The disc is known to AccurateRip, and your rip matches none of the submissions. The second number is the smallest per-track submission count on the disc. |
| `not in database` | AccurateRip returned nothing for this disc. Usually that means no one has submitted this [pressing](glossary.md#pressing), but a lookup that failed reads the same way. The message line under the track table says which. |
| `not checked` | This disc has no completed result yet, either because it has not run or because it failed. |

The CUETOOLS DB panel reports the CTDB comparison:

| Panel text | What it means |
| --- | --- |
| `verified \| confidence 207` | Your audio matches CTDB exactly, and 207 submitted rips stand behind that match. The number counts submissions, not database entries. |
| `damage found \| parity available` | CTDB matched the disc closely enough to locate damage, and holds the recovery data to repair it. **Repair this disc** appears on the card. |
| `no exact match \| 3 entries` | CTDB returned rips for this disc layout, or for a near variant of it, and none of them counts as a match for your disc. Read the number as submitted rips rather than entries, despite the panel's wording. This usually means you have a different [pressing](glossary.md#pressing) with a similar track layout, or damage past what the stored [parity](glossary.md#parity) can rebuild. A rip that matches audio belonging to a variant layout also lands here, because the layouts have to agree too. |
| `not found` | CTDB returned nothing for this disc. As with AccurateRip, a lookup that failed reads the same way; the message line says which. |
| `not checked` | This disc has no completed result yet, either because it has not run or because it failed. |

![The Verify and Repair page showing an album neither database knows. The album verdict reads "Verification complete - review the evidence" with the tally "1 not database-confirmed". The disc chip reads NOT DATABASE-CONFIRMED, the ACCURATERIP panel reads "not in database", the CUETOOLS DB panel reads "not found", and both per-track confidence columns show dashes. A message line under the table reads "AR: disk not present in database, CTDB: database access error: There is an error in XML document (0, 0)."](2026-08-11-verify-fixture-dark.png)

*Two panels, two different reasons. AccurateRip genuinely does not have
this disc, while the CTDB lookup failed outright. The panels look alike;
the message line underneath tells them apart.*

### Track evidence

The TRACK EVIDENCE table lists every track with its
[CRC32](glossary.md#crc32) checksum and its per-track confidence in the
**AR conf** and **CTDB conf** columns. Rows confirmed by at least one
database are shown in green. A `-` means the column has no count for
that track, and the two columns reach it differently: **AR conf** shows
`-` only when AccurateRip has no submissions at all for that track,
while **CTDB conf** shows `-` whenever CTDB gave that track no
confidence, including when it simply did not match.

When it comes to the **AR conf** column, the counts shown there only
match at zero [read offset](glossary.md#read-offset). CD drives usually
read audio with a small fixed shift of samples. That shift could be only
a few samples, say, +6 samples on an ASUS DRW-24B1ST, all the way up to
over 1,000 samples, like the +1292 on an AOPEN DVD RW ISU8424E. So a rip
made without offset correction can show `0/130` on one track and `0/131`
on the next, while the album verdict still reads
`accurate | confidence 4`: the album comparison searches a range of
offsets and found the match at this pressing's offset. When the two
disagree, the album verdict is the one that governs.

![The same page showing a verified album. The album verdict reads "Album verified" with the tally "1 database-confirmed", the disc chip reads DATABASE VERIFIED, the ACCURATERIP panel reads "accurate | confidence 4", the CUETOOLS DB panel reads "not found", and the first five track rows show AR conf values of 0/130, 0/130, 0/131, 0/131 and 0/132 with dashes in the CTDB conf column.](2026-08-12-verify-real-disc-dark.png)

*A real pressing, verified by AccurateRip at its own read offset. Every
track row reads zero at zero offset, and the second number changes from
track to track, because each track has its own submission count.*

The bottom-right corner of the card shows the
[TOC id](glossary.md#toc-id-table-of-contents-id), a fingerprint of the
disc's track layout. Both databases find a disc by its track layout, and
the id shown here is the one CTDB uses.

## The report file

Every completed verify writes an AccurateRip report next to the verified
files, named after the CUE sheet or playlist you verified: for example
`album.accurip` beside `album.cue`. Each disc of a multi-disc set gets
its own report. A `.toc` file listing the disc layout is written beside
it.

The report's first line records the date and the CUETools engine version
(`2.2.6`, the engine this port shares with CUETools for Windows, not the
Linux app's own version number). The body carries the engine's
AccurateRip and CTDB log text, plus the per-track checksums, so it
records exactly what the databases answered that day.

Verifying the same album again writes a new report in its place with the
databases' current answers. When an offline run is later backfilled,
CUETools first tries to save the earlier report under a `.pre-backfill`
name so its answers are kept. Treat that as a convenience rather than a
guarantee: if you want a particular report, copy it somewhere yourself.
See [offline behavior and backfill](offline-and-backfill.md).

## If something goes wrong

### Nothing loads when you drop or pick a selection

The status line explains what CUETools needs. These are some common
messages you might see, with what to do when you see them.

- "Found 12 audio files but no CUE sheet or playlist. Choose the album
  CUE/M3U so track order and disc boundaries are explicit."

  Select the album's CUE sheet or playlist, rather than just the bare
  audio files. If the album has neither, you can write a playlist
  yourself: make a plain text file named `album.m3u` in the same folder,
  listing the audio file names one per line, in track order. Blank lines
  and lines starting with `#` are ignored, so you can leave yourself
  notes in it.

- "Two manifests reference the same audio (album.cue and copy.cue).
  Choose the intended CUE/playlist so CUETools does not have to guess."

  Two CUE sheets, or two playlists, list the same audio files, so
  verifying both would check the same tracks twice. The two named files
  are always the same kind: CUETools uses the CUE sheets in a folder if
  there are any, and only falls back to playlists when there are none.
  Load the one you meant, on its own.

- "Multiple manifests were found, but their disc numbers are missing,
  duplicated, or incomplete. Name/tag them as Disc 1, Disc 2, and so on
  so CUETools does not guess album order."

  In this case you want to number the discs in the file names or tags,
  so you do not end up with a scrambled-egg of an album.

- "Drop one album folder at a time, or select manifest files from one
  album."

  Your selection mixed a folder with something else: two folders at
  once, or a folder dragged along with loose files. Drop one album
  folder on its own, or select only the manifest files from a single
  album.

### A disc card reads FAILED and both panels read "not checked"

The run stopped before a verdict was ready, so neither panel has an
answer to show. Here `not checked` means "no result", not "the databases
do not have this disc". The card's status line names what went wrong.
Fix what it names, then run the verification again.

### Every track shows 0/N in AR conf but the album verdict says accurate

This actually is a verified album, not a contradiction as it may seem.
The per-track column counts zero-offset matches only, so the album
verdict just found the match at the pressing's
[read offset](glossary.md#read-offset). See
[Track evidence](#track-evidence).

### A panel says "not in database" or "not found" and you expected a match

Both texts also appear when the lookup itself did not complete. Read the
message line under the track table: it distinguishes a genuine "disk not
present in database" from an error reaching the service. If it reports
an error, verify the album again once your connection is working.

### The status line ends with "Offline: database verification queued for automatic backfill."

The databases (AccurateRip and CTDB) were unreachable, so only the
database comparison is pending. Your files were indeed fully decoded,
and the report was written. The comparison will re-run automatically the
next time you launch CUETools with the network back online; see
[offline behavior and backfill](offline-and-backfill.md).

## Command line

```console
cuetools-linux /path/to/album
cuetools-linux --verify /path/to/album
```

`/path/to/album` is an album folder, CUE sheet, playlist, or supported
lossless audio file. The first form opens the app with the album loaded
on the Verify & Repair page. Adding `--verify` also starts the run.
Reports are written exactly as in a run started from the window.

## How it works

AccurateRip stores per-track checksums that were submitted by people who
ripped the same disc, keyed by the disc's track layout.
[Confidence](glossary.md#confidence) is a count of independent
submissions that match your audio. The comparison runs at every read
offset in the search range, and the album figure adds up, across those
offsets, the weakest track's match count at each one. A rip matches at a
single offset in practice, so the album number is the weakest track's
agreement there: `accurate | confidence 4` means even the least-matched
track on your disc agrees with four other people's rips. The per-track
table columns report the zero-offset counts instead.

CTDB stores checksums plus [Reed-Solomon](glossary.md#reed-solomon)
recovery data. That extra data lets it do something AccurateRip cannot:
when your audio almost matches an entry, CTDB can locate the damaged
sectors and, within limits, reconstruct them. CTDB finds the read offset
itself while matching, so a rip made without offset correction is not
the reason a disc lands on `no exact match`. The repairable state
appears as `damage found | parity available`, and the repair itself is a
separate action with its own confirmation; see [repair](repair.md).

Verification does not start a repair on its own. The **Repair this
disc** button appears only on a `REPAIRABLE` card, and repair builds a
new sibling copy of the album rather than touching the source files. The
one exception is the `--repair` command-line flag, which is your consent
given up front: a run started that way repairs every repairable disc
without stopping to ask.

## Related topics

- [How Repair rebuilds a damaged album from CTDB parity](repair.md)
- [What happens when you verify offline, and how backfill catches up](offline-and-backfill.md)
- [Which audio formats this build reads and writes](codecs.md)
- [Previewing and applying database metadata with Enrich](enrich.md)
- [Terms used in this manual](glossary.md)
