# Convert an album to another format

Convert re-encodes an album you already have into a different audio
format, for example a FLAC rip into Apple Lossless for an iPhone, or into
MP3 for a car stereo. It reads your existing files and does not change
them. Everything it writes lands in a new album folder: one file per
track, a CUE sheet describing the same disc layout, the album's cover art
when it has any, and a copy of the rip log when the source folder has
one.

## Before you start

| Item | What to know |
| --- | --- |
| Input | One CUE sheet (`.cue`), one `.m3u` or `.m3u8` playlist, one audio file with a CUE sheet embedded in its tags, or one track from an album Convert can recognise in its folder (see below). |
| Result | A new album folder under the output folder you choose, holding the re-encoded tracks and a CUE sheet. |
| Original files | Read only. Convert does not change your source files, their tags, or the folder they sit in. |
| Network | None. Converting does not contact AccurateRip or CTDB. |

This build reads WAV, FLAC, Apple Lossless (`.m4a`), WavPack (`.wv`), and
Monkey's Audio (`.ape`) sources. It writes those five plus MP3, Opus, Ogg
Vorbis, and Musepack. The [codecs page](codecs.md) covers each one and
where its encoder comes from.

Convert handles one album at a time. For a stack of them, use the
[Queue page](queue.md).

## Convert an album

1. Open the **Convert** page.

2. Press **File...** and choose the album's CUE sheet or playlist. The
   picker's **Rip sets** filter lists `*.cue` and `*.m3u`; switch it to
   **All files** if your playlist is a `.m3u8` and you cannot see it.

   The source bar fills in with the path, and the status line at the
   bottom reads "Ready to convert: " followed by that path.

3. Choose where the output goes. Press **Choose folder...** next to
   **Output:** and pick a folder. Leave it alone and the album goes under
   `CUETools` in your Music folder, which is `~/Music/CUETools` on a
   standard setup.

   This row sits in the middle of the page while no result is showing.
   After a conversion finishes it is hidden until you choose the next
   source.

4. Choose the output format. Press the codec button, which shows the
   current choice (`FLAC (.flac) - CUETools managed encoder` on a fresh
   install). The **Choose output codec** window opens with every
   implementation this build knows, lossless formats first and lossy
   after.

   Each row names the format on the left and the concrete implementation
   that would run on the right. A row that cannot run right now says why
   underneath and cannot be selected, so what you can pick is what will
   actually encode. Choose a row and press **Use this codec**.

   Format alone is not the whole choice. FLAC, for instance, has two
   ready rows here: `CUETools managed encoder` and `Reference libFLAC`.
   Both write FLAC; picking one is picking which program does it.

5. If you want to change compression or quality, press **tune**. The
   **Encoder settings** window carries the encoder's
   COMPRESSION / QUALITY setting and every advanced option the codec
   exposes, each with hover text explaining what it does. Close it when
   you are done; the choices are saved with the format.

6. Press **Convert**.

## What happens next

The status line names the current step and the progress bar fills along
the bottom. It reads "Converting to flac..." (whichever format you chose)
while the source is opened, then "Analyzing input file..." as each source
file is read, then "Writing track 07 (29%)..." and so on as each track is
encoded.

The codec button, **tune**, and **Convert** are all disabled while a
conversion runs. There is no Stop button on this page.

The panel in the middle draws the round trip the audio is making: the
source unpacking to [PCM](glossary.md#pcm-pulse-code-modulation) and the
target packing the same PCM again, each side labelled with the bits per
sample it actually achieved on your audio.

![The Convert page during a conversion. The status line at the bottom reads "Writing track 11 (33%)..." with the progress bar about a third full. The codec button, which reads "FLAC (.flac) - CUETools manag...", and the tune and Convert buttons are all greyed out. A ROUND TRIP panel above reads "ALAC -> PCM -> FLAC" with the figures "9.0 -> 9.0 bits/sample" and "same size", over three cards labelled "predict -> unpack 9.0 b/s", "PCM 16.0 b/s" and "encode -> Rice pack 9.0 b/s".](2026-08-13-convertscope-live.png)

*An ALAC album being converted to FLAC. Both sides land on 9.0 bits per
sample, so the two files come out the same size, and the middle card
carries the real decoded audio from your source.*

While it runs, the new album is built in a hidden folder beside its
destination, named `.cuetools-stage-` and a long identifier. Nothing
appears under the name you are expecting until every track is written and
checked. Then the whole folder is moved into place in one step. A
conversion that fails leaves nothing behind at all, not even an empty
album folder.

## Where the files go

Inside the output folder you chose, Convert creates one album folder, for
example:

```text
~/Music/CUETools/
  Beatles, The - Revolver (1966)/
    01 - Taxman.flac
    02 - Eleanor Rigby.flac
    ...
    The Beatles - Revolver (1966).cue
    The Beatles - Revolver (1966).log
    folder.jpg
    .cuetools-complete
```

The album folder is named from the album artist and title, with the year
when the source records one, and a leading "The" moved to the end so
albums sort by name. Tracks are named by number and title.

The CUE sheet describes the same disc layout as the source, so the
converted album can be loaded straight back into [Verify](verify.md).
Its own name keeps the artist as written, without the "The" moved, so
the sheet still reads correctly if you copy it away from its folder.

`folder.jpg` appears when the source album has cover art. Convert uses
the artwork embedded in your source audio files first. When there is
none, it looks in the source folder for `folder.jpg`, `cover.jpg`,
`albumart.jpg`, `thumbnail.jpg`, `albumartlarge.jpg`, `front.jpg`, or
`<album>.jpg`, and failing those for any other `.jpg` or `.png` under
that folder. Whatever it finds is written out as `folder.jpg` and
embedded in the converted tracks, along with the album, artist, title,
and track-number tags.

When the source folder holds a rip log belonging to the album (an
`album.log` beside `album.cue`, the usual Exact Audio Copy layout), that
log is copied into the new folder too, renamed to match the CUE sheet.

`.cuetools-complete` is a marker file, written last, that records the
album folder as finished. The leading dot hides it in most file managers.

Converting the same album twice into the same output folder does not
overwrite the first result. The second one gets a numbered name instead,
`Beatles, The - Revolver (1966) (2)`, and the page reports that path in
its result line.

## Read the result

| What you see | What it means | What to do next |
| --- | --- | --- |
| `Choose a source, pick a format, and convert.` | The page is idle; no source is loaded. | Press **File...**. |
| `Ready to convert: <path>` | The source loaded. | Press **Convert**. |
| `Converting to <format>...` | The run has started and the source is being opened. | Wait. |
| `Writing track 07 (29%)...` | The conversion is running. | Wait. |
| `Conversion complete`, with `Wrote 24 m4a file(s) to <folder>` under it and `done` on the status line | Every track was re-encoded and the album folder is published at the path shown. | Open that folder. |
| `Convert cannot start: choose a ready output codec.` | The selected format has no encoder that can run on this machine, so nothing was attempted. | Press the codec button and choose a row that is not greyed out. |
| `Convert failed: <reason>` | The run stopped and nothing was written. | Read the reason; the next section covers the common ones. |

![The Convert page after a finished run. A large green circle sits above the heading "Conversion complete". Under it a line reads "Wrote 24 m4a file(s) to" followed by a long path ending in "converted-album/Unknown Artist - Unknown Album". The codec button reads "Apple Lossless (ALAC) (.m4a) - ...", the status line at the bottom reads "done", and the progress bar is full.](2026-08-13-convert-alac-complete.png)

*The finished state names the count, the format, and the exact folder. In
this run the source carried no album tags, so the folder fell back to
"Unknown Artist - Unknown Album".*

## If something goes wrong

### The status line reads "Convert failed: is a directory"

You chose an album folder with **Folder...**. Convert reads an album
through its CUE sheet or playlist, not through a folder, so pick that
file with **File...** instead.

If the album has neither, you can write a playlist yourself: make a plain
text file named `album.m3u` in the album's folder, listing the audio file
names one per line, in track order. Blank lines and lines starting with
`#` are ignored.

### The status line reads "Convert failed: Input file doesn't seem to contain a cue sheet or be part of an album."

You picked a single audio file that carries no embedded CUE sheet, and
Convert could not work out which album it belongs to. When a file has no
embedded cue, Convert looks at its folder: two or more tracks of the same
format sharing one album tag are treated as the album, and so is a
playlist that lists them. This error means neither was there. Pick the
album's `.cue` file, or write the playlist described above and pick
that.

### The format you want is greyed out in the codec picker

Read the line under the row. Two reasons are common:

- "The encoder executable is not installed or did not pass its approval
  check." The format is served by a separate program that is not present,
  or whose bytes did not match what this build expects. TAK, OptimFROG,
  and the AAC encoders are among the rows in this position on a stock
  install.

- "The native library could not initialize" followed by the failure name.
  The bundled library for that codec is present but did not load, and the
  reason is in the diagnostic log.

Either way the row stays visible and unselectable rather than failing
later, mid-conversion. Pick a ready row, or see the
[codecs page](codecs.md) for what this build ships.

### The album folder came out "Unknown Artist - Unknown Album"

The source carries no album artist and no album title, so the naming fell
back. Add `PERFORMER` and `TITLE` lines to the CUE sheet, or tag the
audio files if you are converting from a playlist, and convert again.
Load the album on the [Verify and Repair page](verify.md) and press
**Enrich metadata...** on its disc card to fill those tags in from the
databases.

The old folder is not replaced when you retry; see
[Where the files go](#where-the-files-go).

### The Output row disappeared after a conversion

It is hidden while a result is on screen. Choose the next source with
**File...** and the **Output:** row, along with the round-trip panel,
comes back.

## Command line

```console
cuetools-linux --convert /path/to/album.cue
cuetools-linux --convert --convert-to m4a --convert-out /path/to/output /path/to/album.cue
```

`--convert` opens the app on the Convert page. When one of the paths on
the command line is a source Convert can load, the conversion starts on
its own, with no further confirmation.

`--convert-to <format>` picks the output format by its extension, for
example `flac`, `m4a`, `wav`, `wv`, `ape`, `mp3`, `opus`, `ogg`, or
`mpc`. It uses whichever encoder that format currently has selected. A
format this build does not offer is ignored, and the format already
selected on the page is used instead, so check the codec button if the
result is not what you expected.

`--convert-out <dir>` sets the output folder. Without it, the album goes
to the same default the page uses.

## How it works

### A lossless conversion keeps the audio, not the bytes

Converting between two [lossless](glossary.md#lossless) formats decodes
the source to raw samples and re-encodes them. The audio survives
exactly: decode the new files and you get the same samples, in the same
order, as decoding the source.

The files are another matter. A FLAC track and the Apple Lossless track
made from it are different sizes and different bytes, and neither one's
[CRC32](glossary.md#crc32) matches the other's. "Lossless" is a promise
about the audio inside, not about the file.

That distinction is why the databases still recognise a converted album.
AccurateRip and CTDB compare the decoded audio, so a lossless conversion
reaches the same verdict as the album it came from.

### The evidence run

On 2026-08-13 the repaired 24-track FLAC album from the
[repair walkthrough](repair.md) was converted to Apple Lossless on this
build, then loaded back into Verify. The converted set returned
`accurate | confidence 29` and `verified | confidence 207`: the same two
panel readings the FLAC source earned the day before.

![The Verify and Repair page showing the converted Apple Lossless album. The album verdict reads "Album verified" with the tally "1 database-confirmed" and counts of 1 disc, 24 tracks, 1:08:22. The disc chip reads DATABASE VERIFIED, the ACCURATERIP panel reads "accurate | confidence 29", and the CUETOOLS DB panel reads "verified | confidence 207". The first five track rows show AR conf values of 0/86, 0/88, 0/86, 0/87 and 0/86 with CTDB conf values of 224, 229, 230, 230 and 230.](2026-08-13-convert-alac-verified.png)

*The Apple Lossless copy, checked from scratch. Two databases that have
never seen this file agree it holds the same audio as the FLAC album it
was made from.*

### Lossy output is a one-way trip

Opus, Ogg Vorbis, Musepack, and MP3 are [lossy](glossary.md#lossy): they
shrink the file by discarding detail, and that detail is gone. A lossy
copy will not match AccurateRip or CTDB, and this build has no decoder
for any of those four, so a converted lossy album cannot even be loaded
back into Verify here.

Treat lossy output as a copy for a device, and keep the lossless album as
the one you verify, repair, and convert from again later.

### Nothing half-written appears

The album is assembled in a hidden staging folder next to its
destination, and the destination name is reserved while that happens, so
two conversions cannot claim it at once. When every track the encoder
declared has been checked for existence and size, the marker file is
written and the staging folder is renamed into place. A rename on the
same disk either happens or does not, which is why the album folder never
exists in a partial state.

## Related topics

- [Checking an album against AccurateRip and CTDB](verify.md)
- [Which audio formats this build reads and writes](codecs.md)
- [Converting or verifying a stack of albums in one go](queue.md)
- [Filling in album and track tags from the databases](enrich.md)
- [Terms used in this manual](glossary.md)
