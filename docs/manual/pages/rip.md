# Rip a CD

Rip reads an audio CD in your optical drive, encodes it to the format you
choose, and checks the result against AccurateRip and the CUETools
Database (CTDB), two community databases of CD rip checksums. It writes a
new album folder under the output folder you pick, and it never writes to
the disc. If a folder of that name is already there, CUETools leaves it
alone and writes to a numbered sibling instead.

## Before you start

| Item | What to know |
| --- | --- |
| Input | One audio CD in an attached optical drive. |
| Result | A new album folder holding the encoded tracks, a CUE sheet, a rip log, an AccurateRip report, a `.toc` file, and a small machine-readable receipt. |
| Original files | Nothing you already have is changed. An album folder that already exists is never written into. |
| Network | The disc lookup, the cover search, and the AccurateRip and CTDB checks each need a connection. The read and the encode finish without one. |
| Time | A Rip reads the disc once. **Test & Copy** reads it twice, or three times when two reads disagree, so plan for two to three times as long. |

Your user account needs access to the drive. On most distributions that
means membership of the `cdrom` group, which you grant once:

```console
sudo usermod -aG cdrom "$USER"
```

Log out and back in afterwards, because group membership is applied at
login. Without it the drive is listed but cannot be opened, and the page
reports no disc even with a disc loaded. See the
[install page](install.md).

The first Rip, Verify, or Test & Copy on a drive runs a
[drive calibration](glossary.md#drive-calibration) before it reads
anything. It needs the audio disc in the drive, takes a few seconds, and
its result is saved, so later rips on the same drive skip it.

FLAC, Apple Lossless (`.m4a`), WavPack, Monkey's Audio (`.ape`), and MP3
have encoders in every build. Ogg Vorbis, Opus, and Musepack come from
the bundled command-line encoders. The [codecs page](codecs.md) lists
what this build writes.

## Rip a disc

1. Open the **Rip** page from the left rail.

2. Pick the drive in the **DRIVE** box at the top. Drives are listed by
   letter, in kernel order: `A` is `/dev/sr0`, `B` is `/dev/sr1`, and so
   on. The drive's model appears beside the letter as soon as it answers,
   with no disc needed.

3. Put the disc in and close the tray. CUETools polls the drive every two
   seconds and reads a newly loaded disc on its own. You can also press
   **Read disc**, which closes an open tray first, or **Eject**, which
   becomes **Close** while the tray is out.

4. Check what the disc was identified as. The album title, artist, and
   track list fill in, along with a line reading, for example,
   `12 tracks   51:07   3 release match(es)`. The status line names the
   choice it made: "Identified: Artist - Album. Ripping comes next."

   When more than one release matches the disc, the **release** box lists
   them. Each row starts with the source it came from, so a title burnt
   onto the disc itself shows as `CD-Text`. Pick a different row and the
   album, the track titles, and the folder the rip is written to all
   change to match. Pick before you start; the choice is frozen when the
   job begins.

   A disc no database knows still rips. The status line then reads "Disc
   read; not found in the metadata databases (generic track names)." and
   the tracks are numbered rather than named.

5. Set up the output in the **OUTPUT** panel on the right.

   - The top button names the format that will be written, as
     `FLAC (.flac)`. Press it to open the **Choose output codec**
     window, which lists every implementation this build has, lossless
     first. A row that cannot run says why underneath and cannot be
     chosen. The button names only the format, so hover it to see which
     implementation is selected. Compression and quality settings for a
     format live on the [Convert page](convert.md); both pages share one
     configuration.
   - The path box is where albums go. Press **...** to browse. Left
     alone it is `~/Music/CUETools`.
   - **cue** writes a CUE sheet beside the tracks, and **log** writes the
     ripper log. Both are on by default.
   - **layout** chooses `Tracks` (one file per track) or
     `Image + embedded CUE` (a single audio file with the cue sheet
     inside it).
   - **Artwork...** opens **Choose artwork**, listing every cover found
     for the chosen release with its provider, size, and why it matched.
     Pick one and press **Use this artwork**. CUETools selects an
     eligible front cover on its own, so you only need this window to
     override that choice.

6. Choose how carefully the disc is read, in the **quality** box.
   `Secure` is the default.

   | Mode | What the drive does | When to use it |
   | --- | --- | --- |
   | `Burst` | Reads each stretch of the disc once. A stretch that reports errors is re-read, up to 16 passes in total. Deep recovery never runs, and the drive's cache is not flushed between passes. | A clean disc you want quickly, when you are happy to let the database check be the only confirmation. |
   | `Secure` | Reads every stretch at least twice and requires the passes to agree. A stuck stretch gets at least 32 passes, and up to 252 while the error count is still falling. On a drive that [caches re-reads](glossary.md#cache-defeat), the cache is flushed before each re-read. | Normal ripping. This is the default. |
   | `Paranoid` | The same as Secure, but three passes must agree, and a stuck stretch gets at least 64 before the deep-recovery extension applies. | Discs that matter and read slowly, when time is not the constraint. |
   | `Salvage` | Takes effect only through **Test & Copy**. It reads at Burst quality, pinned to the drive's slowest speed, and conceals the samples the passes never agreed on. The result is labelled `salvaged`. | Discs that no drive reads repeatably, where a listenable capture beats no capture. |

   Selecting `Salvage` and pressing **Rip** or **Verify only** does not
   produce a salvage capture. Those two buttons run at Burst quality
   instead, and their output is not labelled salvaged.

7. Press **Rip**. It becomes available once a disc is loaded and the
   cover search has settled.

   **Test & Copy** and **Verify only** sit under it, and
   [Test & Copy](#test--copy-reads-the-disc-twice) is described below.
   **Verify only** reads and checks the disc without encoding anything:
   no album folder, no audio files, just the verdict, the per-track
   evidence, and a row in your rip history.

   **Stop** asks the running job to stop at the next safe point. A
   stopped Rip publishes nothing and leaves no album folder behind.

## What happens next

If this drive has not been calibrated, or its saved calibration is out of
date, the status line reads "Calibrating drive before its first read..."
or "Refreshing drive calibration..." before the read starts.

The status line then names the phase and the progress: "Ripping... 47%",
or "Verifying... 47%" for a Verify only. A Test & Copy prefixes every
message with the read it is on, as in "Copy read (2 of 2): Ripping...
47%". The progress bar under the disc map tracks the same figure, and the
last two percent are reserved for checking and publishing the encoded
files.

Three live displays sit in the **RUN** panel:

- The disc map fills from the centre outward as the read progresses,
  which is the direction a CD is read.
- The pair of needles is the music's loudness, measured per channel.
- The trace beside it is the read speed, with the figure printed
  underneath as a multiple of realtime, for example `6.4x`.

When a stretch of the disc will not agree with itself, a box appears
showing the literal counts from the drive: `x4` for the number of extra
passes so far, `3 sectors disagree` beside it, and a line under the box
reading `42% in` for where on the disc it is. The box turns green and reads
`recovered` when the passes finally agree.

If the drive gives up on a stretch, the counts stop and the box fades
like any other. The unreadable stretch is not treated as clean: it is
what turns a Test & Copy result from `verified` into `consistent`, and
what adds `(damage recorded)` to the job's row in your rip history.

When the rip finishes, a **DONE** panel appears with the summary, an
**Open folder** button, and **Dismiss**. Ejecting the disc does not clear
it.

## Read the result

The **DONE** panel's first line summarises the job. A Rip reads like
this:

```text
Ripped 12 flac files  .  AccurateRip verified (confidence 4)  .  CTDB confidence 207  .  final output PCM verified after metadata
```

Each part is separate evidence, and each can differ:

| Part of the summary | What it means |
| --- | --- |
| `Ripped 12 flac files` | The album was written and published. The number is the audio files the encoder produced. |
| `AccurateRip verified (confidence 4)` | AccurateRip matched every track. The number is the weakest track's [confidence](glossary.md#confidence): even the least-matched track agrees with 4 other people's rips. |
| `not found in AccurateRip` | AccurateRip did not confirm this rip. That covers a disc nobody has submitted, a rip that matched nothing, and a lookup that never completed. |
| `CTDB confidence 207` | CTDB matched your audio exactly, and 207 submitted rips stand behind that match. The number counts submissions, not database entries. |
| `CTDB can repair 12 damaged sector(s)` | CTDB found damage and holds [parity](glossary.md#parity) for it. See [Repair a rip CTDB can fix](#repair-a-rip-ctdb-can-fix). |
| `final output PCM verified after metadata` | The encoded files were decoded again after tagging and matched the audio that came off the disc. |
| `WARNING: final output not verified` | The encoder that ran does not carry that check, so the written files were not decoded back. The audio still passed the read checks. |
| `lossy output` | The chosen format discards audio by design, so decoding it back could not match anyway. See [lossy](glossary.md#lossy). |

A Test & Copy summary is built differently:

```text
Test & Copy: 12 flac files, verified after 2 reads (at least 2 agreed per track)
```

| Part of the summary | What it means |
| --- | --- |
| `verified after 2 reads` | Two independent reads agreed on every track, and no damage was recorded. |
| `consistent after 3 reads` | Two reads agreed on every track, but damage was recorded as well, so the reads match without being clean. It is followed by `; CTDB repair required for 12 sector(s)` or `; read damage remains`. |
| `salvaged (drive-stable) after 2 reads` | A Salvage capture whose reads agreed. Agreement here shows the drive returned the same bytes twice; it is not a claim that the bytes match the disc. |

The line under the summary carries the same outcome in a sentence, and
names what each database said, for example "Test & Copy verified after 2
reads; at least two agreed per track.  Also AccurateRip-accurate
(confidence 4).  Final output PCM was decoded and verified after metadata
finalization."

A Salvage capture says what it is in three places: the summary above, the
status line ("Test & Copy salvaged (drive-stable capture, not verified)
-> " and the path), and its row in your rip history, which reads
`salvaged` or `salvaged (damage recorded)` where an ordinary job would
read `verified`.

### The database line

At the bottom of the disc panel, two short readings report what
AccurateRip and CTDB answered.

| Reading | What it means |
| --- | --- |
| `AR not checked` / `CTDB not checked` | No result yet. It reads this at startup, and again from the moment a job starts until that job's first database answer. After a job finishes, its readings stay on screen, even if you swap the disc, until the next job begins. |
| `AR 4 / 130  accurate` | Every track matched. 4 is the weakest track's confidence, 130 the smallest per-track submission count on the disc. |
| `AR 0 / 82` | AccurateRip holds rips for this disc layout and none of them matched yours. |
| `AR 0 / 0` | AccurateRip returned nothing to compare against, either because it has no record of this [pressing](glossary.md#pressing) or because the lookup did not complete. |
| `CTDB match . conf 207` | CTDB matched your audio exactly, with 207 submissions behind the match. |
| `CTDB recoverable damage . 12 sector(s)` | CTDB located damage and holds the parity to rebuild those sectors. |
| `CTDB 0 / 235` | CTDB returned rips for this disc layout and none of them counts as a match. Read the second number as submitted rips. |
| `CTDB 0 / 0` | CTDB returned nothing to compare against. |
| `CTDB found, no exact match` | The per-read wording of `CTDB 0 / 235`: CTDB answered with entries for this disc layout and none matched. It appears only while a Test & Copy reports each read. |

During a Test & Copy the same line is rewritten as each read finishes, so
you see what the Test read found before the Copy read starts. Those
per-read readings can also say `lookup failed`, which means that database
could not be reached or answered with an error, and never judged the
disc. `not in database` and `not found` mean the opposite: the database
answered and has no record of this pressing. When a Test & Copy
publishes, the line settles back to the counts above. A held or failed
run does not rewrite it, so the last read's wording stays on screen until
the next job starts.

### The track rows

Each track is one row: number, title, length, then four narrow columns.
The first two are per-track counts from AccurateRip and from CTDB, with
`-` where that database gave the track no count. The last two are
[CRC32](glossary.md#crc32) checksums of the track's audio, shown as eight
hexadecimal digits.

Those two CRC columns are the Test and Copy roles. A **Verify only** run
fills the Test column, a **Rip** fills the Copy column, and a Test & Copy
fills both. Neither role erases the other, and both are kept per disc, so
inserting a disc you read before brings its earlier checksums back into
the grid before you read it again. A value followed by `x3` has been seen
in three separate jobs.

Per-track AccurateRip counts are zero-offset counts, so a rip made on a
drive with a [read offset](glossary.md#read-offset) can show `-` in every
row while the album line still reads `accurate`. The album line is the
one that governs; the [Verify page](verify.md#track-evidence) explains
why in more detail.

### The history line

Beside the database readings, one line reports how this read compares
with your own earlier reads of the same disc. This is a local check
against `~/.config/CUETools2026/verify-history.json.gz`, and it is
independent of both databases.

| Line | What it means |
| --- | --- |
| `First read of this disc - recorded to your verify history.` | Nothing to compare against yet. |
| `Consistent with your 2 earlier read(s) - bytes match.` | This read produced the same audio as your earlier ones. |
| `DIFFERS from your earlier read on 3 track(s) - investigate.` | The same disc read differently this time. Something changed: the disc, the drive, or the reading conditions. |
| `This job completed, but verify history could not be saved.` | The audio is fine; the local history file could not be written. |

With no disc in the drive, the page lists your recent jobs under
**RECENTLY RIPPED**, each with its grade and what the written files
proved. A rip reads "rip - verified (AR 4, CTDB 207); final output PCM
verified after metadata"; a Test & Copy reads "test & copy (2 reads) -
verified after 2 optical reads; at least 2 agreed per track (AR 4,
CTDB 207)".

## Test & Copy reads the disc twice

**Test & Copy** reads the whole disc twice and writes only what two
independent reads agree on, track by track. The first read is evidence
only; the second read is the one that gets encoded. If the two disagree
anywhere, a third read runs and the resolution is tried again.

It will not start unless calibration established that a re-read reaches
the disc rather than the drive's memory, and on a drive that caches, the
[cache is flushed](glossary.md#cache-defeat) between reads. Without that,
two reads agreeing would prove nothing.

It always reads at Secure quality or better, whatever the **quality** box
says, with the single exception of `Salvage`, which deliberately reads at
Burst. Its codec is health-checked and frozen before the first read, so
the format cannot change halfway through.

When one read agrees with another on every track, that read's files are
published whole. The album folder gets an extra
`<Artist - Album (Year)> - Test & Copy.log` recording the reads, the
per-track agreement, and the parity figures.

### If the copy is held

If no single read agrees with another on every track, nothing is
published and a **HELD** panel appears instead of **DONE**:

```text
Held - The Test and Copy CRCs disagree on track(s) 4, 9. Nothing was written. The completed Copy is retained; re-run, accept it anyway, or discard.
```

The completed Copy read is kept in a temporary folder under `/tmp/cuetc/`
by default rather than deleted, because on a dying disc it may be the only complete
read you get. The message names a reason first when there is one, for
example "Confirming read failed: " with the error, or "Stopped during the
confirming read.".

You have three ways out:

- Read the disc again by pressing **Test & Copy**. The held copy is kept
  until the new run produces a result, so a re-run that fails early
  leaves you no worse off.
- Press **Accept copy anyway**. The Copy read is written to your output
  folder and flagged in its log as `NOT test-verified - accepted by user
  without agreement.`, with the disagreeing tracks named. The status line
  confirms: "Copy read accepted anyway - written and flagged NOT
  test-verified."
- Press **Discard held copy**. The staged reads are deleted and the
  status line reads "Discarded - nothing was written."

Ejecting the disc does not throw the held copy away. It is parked, and
the status line says so: "The held Test & Copy is parked - reinsert the
same disc to resume it. A different disc frees it." Loading a different
disc, or starting a new job, releases it.

## Repair a rip CTDB can fix

When CTDB finds damage it holds [parity](glossary.md#parity) for, a
button appears in the **DONE** panel whose label reads, for example:

```text
CTDB parity can recover 12 damaged sector(s). Repair creates and independently verifies a sibling copy; this rip stays unchanged. Worst parity stripe uses 3 of 4 correctable errors.
```

Read the last sentence as headroom. When the worst
[parity stripe](glossary.md#parity-stripe) is at capacity the label says
so, and adds "prefer a re-rip if the disc still reads", because one more
error in that position would have been beyond repair.

Pressing it asks for confirmation, then builds a repaired copy in a new
folder beside the rip and verifies that copy from scratch before keeping
it. Your rip is not touched. On success the status line reads "CTDB
repaired copy verified -> " and the new path. The
[Repair page](repair.md) covers what the repaired folder contains.

One case has no button: a multi-track rip with no CUE sheet has no single
album input to repair from, so the status line says "CTDB repair is
available, but this multi-track output has no album cue." Leave the
**cue** box ticked to avoid it.

## What the album folder contains

An album ripped to FLAC with the default settings looks like this, where
the stem is the artist, album, and year the release supplied:

| File | What it is |
| --- | --- |
| `01 - Track title.flac` and so on | The encoded tracks, one per track, or a single file in `Image + embedded CUE` layout. |
| `<stem>.cue` | The CUE sheet describing the disc layout. Written when **cue** is ticked. |
| `<stem>.log` | The ripper log. Written when **log** is ticked. |
| `<stem>.accurip` | The AccurateRip report for this rip. |
| `<stem>.toc` | The disc's table of contents. |
| `<stem> - Test & Copy.log` | Written by Test & Copy only: the reads used, per-track agreement, and parity figures. |
| `folder.jpg` | The cover art, when a cover was found for the release. |
| `rip.verify` | A machine-readable record of the read: per-track checksums, the drive, its read offset, and the quality mode the disc was actually read at. |
| `.cuetools-complete` | Written last. Its presence marks a folder that was published complete. |

The album folder is named from the release, as
`Album Artist - Album Title (1997)`, with a bracketed descriptor added
for a multi-disc set, a live album, an EP, and so on. A leading article
in the artist name is moved to the end so folders sort sensibly, which
turns "The Beatles" into "Beatles, The".

Nothing appears at the final path until the whole album is ready.
CUETools writes it into a hidden sibling folder first and renames that
into place in one step, so an interrupted rip cannot leave a half-written
album where a complete one should be.

## If something goes wrong

### The page says no disc, and there is a disc in the drive

Check drive access first. Without membership of the `cdrom` group the
drive can be listed and still refuse to open, and the app cannot tell
that apart from an empty tray. Run
`id -nG | tr ' ' '\n' | grep -x cdrom`; an empty answer means the group
is missing, or that you have not logged out and back in since adding it.

A disc that is not an audio CD is reported as such, naming what the drive
says is loaded, for example "Not an audio CD - DVD-ROM in drive A:".

### "This physical drive is locked to its existing job."

Another CUETools window owns that drive. One physical drive is claimed by
one job at a time, across windows, so the second claimant is refused
before the hardware is touched. Use the window that is running the job,
or pick a different drive.

### Choosing another drive during a rip opens a second window

That is deliberate. The running job stays bound to its own drive, and the
drive you picked opens in a separate CUETools window with its own Stop
button, status line, and evidence. The status line confirms: "Drive B:
opened in an isolated CUETools window. This A: job continues here." The
second window is titled `CUETools Linux - Drive B` and starts on the Rip
page.

### "Calibration could not prove an independent re-read strategy"

Secure and Paranoid depend on re-reads that genuinely hit the disc, so
they will not start unless calibration established a way to defeat the
drive's cache. The message ends "so Secure/Paranoid reading cannot
start." Retry the operation with the disc loaded, since calibration needs
an audio disc; if the drive still cannot prove it, `Burst` will read and
the database check still applies.

Two other calibration messages stop a job before it reads: "No audio disc
was ready for drive calibration." means the drive had no audio disc when
the gate ran, and "Saved drive calibration is unreadable; repair or
remove it before reading." refers to
`~/.config/CUETools2026/drive-calibration.json`, which you can delete to
force a fresh calibration.

### "rip cannot start" with a codec message

The selected encoder is not ready, and the message names it and says why,
or reads "no encoder is configured for `<format>`. Open the codec picker
to choose a ready encoder." Open the codec button and pick a row that can
run. The check happens before the drive is claimed, so nothing was read.

### The drive rejects every read, or the rip fails part way through

A rip that fails reports the drive's own error on the status line and
records the full detail in `~/.config/CUETools2026/logs/`.

One failure has a specific cure. When the drive starts rejecting every
read shape, down to single sectors, in regions it read a moment earlier,
CUETools stops and says so:

> The drive is rejecting every read shape, down to single sectors, in
> regions it read successfully before. This stuck state has been observed
> on USB drives after extended recovery of damaged media; live
> characterization (2026-08-14) showed it survives every software reset
> and even a cable replug, and only a full power cycle has cleared it.
> Power the drive off and on (for external drives, replugging the USB
> cable alone may not be enough), then retry; the disc and any completed
> evidence are unaffected.

Do exactly that. This is a firmware state that survives unplugging the
data cable, so the drive needs to lose power. The disc is unharmed, and a
completed Test & Copy read that was already held stays held.

### An album is missing, and a `.cuetools-incomplete-...` folder is not

When a rip fails after the audio was read, the partly written album is
kept under a dated `.cuetools-incomplete-` name in your output folder
rather than deleted, and the error names the path. It is a hidden folder,
so `ls -a` shows it. Keep it if you want the audio that was read;
otherwise delete it.

A stopped rip is different: it publishes nothing and leaves nothing
behind.

### The album went into a folder ending in " (2)"

The name it wanted was already taken, or reserved by another running job.
CUETools never writes into an existing album folder, so it took the next
free name and logged "output folder is occupied or reserved".

### The rip finished but no database confirmed it

With no network the read, the encode, and the checksums all still
complete, and the album is written; the databases simply gave no verdict,
which shows as `AR 0 / 0` and `CTDB 0 / 0`. A rip is not queued for
automatic backfill the way a verification is. To get the database
verdict later, load the finished album on the
[Verify & Repair page](verify.md) and verify it there.

## Command line

```console
cuetools-linux --rip-page
```

Opens the app on the Rip page instead of the Verify page, with your usual
settings. There is no command-line flag that starts a rip; ripping is
started from the page.

## How it works

Calibration runs once per drive and is saved under
`~/.config/CUETools2026/drive-calibration.json`, keyed by the drive's own
identity string. It measures the drive's supported speed range, whether
the drive answers a re-read from its cache instead of the disc, and how
many bytes have to be read elsewhere to evict that cache. Every mode goes
through this gate, but they ask different things of it: Burst proceeds
whatever the answer, while Secure and Paranoid refuse to start unless the
record proves an independent re-read is possible. A drive that has ever
demonstrated caching keeps the largest flush size ever proven for it,
even if a later, noisier calibration fails to see the caching again.

That matters because of what a secure read is. Reading a stretch twice
only proves something if the second read comes off the disc. On a caching
drive the second read can be the first read handed back from memory,
which would agree with itself no matter what happened at the disc
surface. So on those drives CUETools reads an unrelated region first,
large enough to push the wanted audio out of the cache, before each
re-read.

Deep recovery, on by default, is what lets Secure and Paranoid keep
working a damaged stretch past their normal pass cap. It extends the
re-reads while the error count is still improving, up to 252 passes, and
drops the drive to its slowest speed on a stuck stretch, because slow
rotation tracks marginal pits better. It never runs at Burst quality.

Adaptive read speed, also on by default, starts at the drive's calibrated
maximum, steps down when the drive gets stuck, and eases back up over
clean stretches. Speed changes are requested only at safe boundaries, and
the audio is identical at any speed.

Test & Copy is a different kind of proof. AccurateRip and CTDB can only
confirm a disc other people have already submitted. Two independent reads
of your own disc agreeing bit for bit is evidence you can get for a
pressing nobody else has, and it is the reason a Test & Copy publishes
one read's folder whole rather than assembling tracks from several reads.

Nothing on this page sends your rip to any database. AccurateRip and CTDB
are read, never written: verifying an unknown pressing contributes
nothing back, and no rip you make is submitted anywhere.

Three expert settings on this page have no interface yet and live in
`~/.config/CUETools2026/settings.txt`, which you edit with the app
closed: `WpfStopOnUnrecoverable` (off by default; when on, the rip stops
at the first stretch the drive cannot recover instead of carrying on and
marking it), `WpfDeepRecovery`, and `WpfAdaptiveReadSpeed`.

## Related topics

- [Checking an album you already have against the databases](verify.md)
- [How Repair rebuilds a damaged album from CTDB parity](repair.md)
- [Which audio formats this build reads and writes](codecs.md)
- [Re-encoding a finished rip into another format](convert.md)
- [What happens when you verify offline, and how backfill catches up](offline-and-backfill.md)
- [Terms used in this manual](glossary.md)
