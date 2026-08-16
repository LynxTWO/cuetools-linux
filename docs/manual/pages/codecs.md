# Audio formats this build reads and writes

This build reads five lossless audio formats and writes nine. Reading a
file never changes it. Everything the app writes goes into new files:
[Convert](convert.md) and [Rip](rip.md) each say where theirs land.

A [codec](glossary.md#codec) is the program behind a format, and one
format can have more than one. That is why the app asks you to pick an
implementation and not just an extension.

## Formats this build reads

Verify, Repair, Convert, and the Queue all decode through the same set.

| Format | Extension | Where the decoder comes from |
| --- | --- | --- |
| FLAC | `.flac` | Compiled into the app |
| Apple Lossless (ALAC) | `.m4a` | Compiled into the app |
| WAV (PCM) | `.wav` | Compiled into the app |
| WavPack | `.wv` | Packaged WavPack 5.9.0 library |
| Monkey's Audio | `.ape` | Packaged Monkey's Audio SDK 13.20 library |

There is nothing to choose on the reading side. The app picks the decoder
itself, and the **Choose output codec** window is about output only.

Whole albums are loaded through a CUE sheet (`.cue`) or a playlist
(`.m3u` or `.m3u8`), or as one audio file with a CUE sheet in its tags.
Both playlist extensions are read.

## Formats this build writes

Every implementation below can be selected on a stock install. The picker
groups
[lossless](glossary.md#lossless) formats first and
[lossy](glossary.md#lossy) after.

| Format | Extension | Implementations you can pick | Type |
| --- | --- | --- | --- |
| FLAC | `.flac` | `CUETools managed encoder`, `Reference libFLAC` | lossless |
| WavPack | `.wv` | `WavPack library` | lossless |
| Apple Lossless (ALAC) | `.m4a` | `CUETools managed encoder` | lossless |
| Monkey's Audio | `.ape` | `Monkey's Audio SDK` | lossless |
| WAV (PCM) | `.wav` | `CUETools managed encoder` | lossless |
| Opus | `.opus` | `opusenc.exe` | lossy |
| Ogg Vorbis | `.ogg` | `oggenc.exe` | lossy |
| Musepack | `.mpc` | `mpcenc.exe` | lossy |
| MP3 | `.mp3` | `LAME CBR`, `LAME VBR` | lossy |

The four lossy formats are output only. This build has no decoder for any
of them, so an album you convert to MP3, Opus, Ogg Vorbis, or Musepack
cannot be loaded back into Verify, Repair, or Convert here. Keep the
lossless album as the copy you verify and convert from.

The five lossless formats are also the five it reads, so a lossless
conversion can be verified against the databases (AccurateRip and CTDB)
exactly like the album it came from.

### Rows the picker shows and cannot run

Nine more implementations appear in the window and stay greyed out on a
stock install, because the program each one needs is not part of this
build:

| Format | Implementation |
| --- | --- |
| FLAC (`.flac`) | `flac.exe` |
| Apple Lossless (ALAC) (`.m4a`) | `ffmpeg.exe` |
| TAK (`.tak`) | `takc.exe` |
| OptimFROG (`.ofr`) | `ofr.exe` |
| AAC (qaac) (`.m4a`) | `qaac.exe (tvbr)` |
| M4A (`.m4a`) | `neroAacEnc.exe` |
| xHE-AAC (exhale) (`.m4a`) | `exhale.exe` |
| MP3 (`.mp3`) | `lame.exe (CBR)`, `lame.exe (VBR)` |

Each carries the same line underneath: "The encoder executable is not
installed or did not pass its approval check." They are listed rather
than hidden so the window shows everything the app knows about, and none
of them can be selected. This build has no Settings page and no import
button, so there is no way to supply one of these programs from inside
the app.

## Choosing the implementation, not just the format

The codec picker is the same window on the Convert, Rip, and Queue pages.
Press the button that names the current choice. The **Choose output
codec** window opens, headed "Output codec", and lists every
implementation this build knows about.

Each row names the format on the left and the implementation on the
right, so `FLAC (.flac)` with `Reference libFLAC` and `FLAC (.flac)` with
`CUETools managed encoder` are two separate rows. Both write valid FLAC.
Picking one is picking which program does the work. A row that cannot run
says why underneath and cannot be selected. Choose a row and press **Use
this codec**.

A packaged native row also shows the version of the library it loaded:
`1.5.0` under Reference libFLAC, `5.9.0` under the WavPack library, and
`3.100` under both LAME rows. The Monkey's Audio row shows `1` there
instead of a version number; the library behind it is the pinned 13.20
SDK either way.

![The Convert page with no source loaded. The output button at the top reads "FLAC (.flac) - CUETools manag...". The page body reads "Pick a .cue, an album folder, or a file with an embedded cue, choose an output format, and CUETools re-encodes every track. Only formats with a working encoder in this build are offered." Below it a panel headed FLAC carries the line "fixed / LPC predictor, then Rice-coded residual", the figures "5.1 bits/sample" and "~32% of PCM", and four cards labelled signal, predict, residual, and Rice pack.](2026-08-13-codecscope-idle.png)

*The panel under the page names what the selected codec does to audio.
The figures come from a built-in demo signal, not from your album. This
screenshot is from an older build, whose sidebar had no Rip or Queue
page.*

On the Convert and Queue pages the button carries both halves, as
`FLAC (.flac) - CUETools managed encoder`. On the Rip page it carries
the format alone, as `FLAC (.flac)`, so hover it to see which
implementation is selected.

![The Convert page after a finished run. The output button reads "Opus (.opus) - opusenc.exe". A large green circle sits above the heading "Conversion complete", and the line beneath reads "Wrote 24 opus file(s) to" followed by a path ending in "opus-out/Unknown Artist - Unknown Album". The status line at the bottom reads "done" and the progress bar is full.](2026-08-13-opus-convert-complete.png)

*The button names the exact program that encoded the album. `opusenc.exe`
is a Linux binary despite the name: the app identifies command-line
encoders by a fixed file name shared with the Windows build. This
screenshot is from an older build, whose sidebar had no Rip page.*

## Compression and quality settings

Press **tune** beside the codec button on the Convert page to open the
encoder settings, which carry the COMPRESSION / QUALITY setting and the
codec's advanced options. For FLAC and MP3, the two formats with more
than one runnable implementation, the settings belong to whichever
implementation is selected. The Convert page is the only page with a
**tune** button. Settings belong to the implementation, not the format,
and they are written to `~/.config/CUETools2026/settings.txt` when the
app closes, so the Queue and Rip pages encode with whatever is saved for
the implementation they run.

A fresh install starts here:

| Implementation | Settings offered | Starts at |
| --- | --- | --- |
| FLAC, either implementation | `0` to `8` | `5` |
| Apple Lossless | `0` to `10` | `5` |
| WavPack library | `fast`, `normal`, `high`, `high+` | `normal` |
| Monkey's Audio SDK | `fast`, `normal`, `high`, `extra`, `insane` | `high` |
| LAME VBR | `V9` (smallest) to `V0` (best) | `V2` |
| LAME CBR | `96` to `320` kbps | `256` |
| Opus | `6` to `256` kbps | `192` |
| Ogg Vorbis | `-1` to `8` | `8` |
| Musepack | `0` to `10` | `7` |

On a lossless format the setting changes only how hard the encoder works
and how small the file comes out. The audio is identical at every
setting, so a file made at the fastest setting decodes to the same
samples as one made at the strongest. WAV has no setting at all: it
stores every sample as it is.

## If something goes wrong

### The format you want is greyed out in the codec picker

Read the line under the row. On a stock install the nine implementations
in [Rows the picker shows and cannot
run](#rows-the-picker-shows-and-cannot-run) are always in this state, and
they all read "The encoder executable is not installed or did not pass
its approval check." Pick a row that is not greyed out.

The same line also appears if a packaged encoder was replaced or damaged
after installation, because the app compares each packaged program
against the checksum that shipped with it and refuses a file that does
not match.

### A disc card reads FAILED with "Unsupported audio type"

The album's audio is in a format this build has no decoder for. The full
line names the file, for example "Unsupported audio type:
/music/album/track01.tta".

The Verify page's **File...** picker offers a "Supported lossless audio"
filter listing `flac`, `wv`, `ape`, `tak`, `m4a`, `tta`, `wav`, `ofr`,
`wma`, and `aiff`, which is wider than what this build can decode. TTA
and OptimFROG albums both fail with this exact message, naming the
`.tta` or `.ofr` file. A TAK album loads the same way and then fails
with a different message, naming `takc.exe`, the program this build does
not ship. WMA and AIFF do not load at all.

Nothing in the app changes that. If you still have the disc, rip it again
to one of the five formats in
[Formats this build reads](#formats-this-build-reads).

### An audio file you pick does not load at all

The status line reads "No CUE sheet, playlist, or supported lossless
audio was found." The file's extension is outside the set the picker
accepts at all: `.flac`, `.wv`, `.ape`, `.tak`, `.m4a`, `.tta`, `.wav`,
and `.ofr`. Anything else, including `.mp3`, `.wma`, `.aiff`, `.ogg`,
`.opus`, and `.mpc`, stops here. Nothing loaded, so there is nothing to
verify or convert from that file.

### A packaged codec is missing from the picker entirely

WavPack, Monkey's Audio, Reference libFLAC, and the two LAME rows come
from packaged libraries. Each library is checked before it is loaded, and
a codec whose library fails that check is not registered at all, so its
rows never appear rather than appearing and failing mid-encode. A
WavPack or Monkey's Audio file also stops being readable when that
happens.

The reason is recorded in the diagnostic log in
`~/.config/CUETools2026/logs/`, under the `codecs` category, as
`native codec unavailable:` followed by the file name and the reason.
Reinstalling the package restores the packaged files.

## Where the codec files live

Both packages carry identical codec payloads, in two folders beside the
program:

| Folder | Holds |
| --- | --- |
| `native/` | The four packaged libraries (`libFLAC_dynamic.so`, `wavpackdll.so`, `MACLibDll.so`, `libmp3lame.so`) and `native-codecs.json`, which records each one's version, licence, source, size, and SHA-256 checksum. |
| `encoders/` | The three command-line encoders (`opusenc.exe`, `oggenc.exe`, `mpcenc.exe`) and `linux-encoders.json`, which records each one's version, licence, size, and checksum. |

From the `.deb` those are `/usr/lib/cuetools-linux/native/` and
`/usr/lib/cuetools-linux/encoders/`. The AppImage carries the same two
folders inside itself.

The licence text for every packaged codec, and the written offer of its
source, ship with the app: `/usr/share/doc/cuetools-linux/`, holding
`THIRD-PARTY-NOTICES.md` and a `licenses/` folder. The AppImage carries
the same files at the same path inside itself.

## How it works

### Every packaged codec is checked before it is loaded

At startup the app reads `native-codecs.json`, hashes each library file,
and compares it with the checksum recorded there. Only an exact match is
registered and loaded, by its full path. There is no search of your
system directories, and no fallback to a library of the same name found
elsewhere, so the codec that runs is the one that shipped. A library that
fails leaves its codec unregistered and writes one line to the diagnostic
log; the other codecs are unaffected.

Command-line encoders go through the equivalent check against
`linux-encoders.json` before a row is offered as ready.

### Where the packaged codecs come from

They are built from pinned upstream sources rather than taken from your
distribution, so they cannot change underneath the app when your system
updates.

| Component | Version | Licence |
| --- | --- | --- |
| libFLAC | 1.5.0 | BSD-3-Clause |
| WavPack | 5.9.0 | BSD-3-Clause |
| Monkey's Audio SDK | 13.20 | BSD-3-Clause |
| LAME | 3.100 | LGPL-2.0-or-later |
| opusenc | opus-tools 0.2 with libopusenc 0.3, libopus 1.6.1, libogg 1.3.6, CUETools patches | BSD-2-Clause and BSD-3-Clause |
| oggenc | vorbis-tools 1.4.2 with libvorbis 1.3.7, libogg 1.3.6 | GPL-2.0 (oggenc), BSD-3-Clause (libraries) |
| mpcenc | Musepack r495 with the CUETools patch, SV8 | LGPL-2.1-or-later and BSD-3-Clause components |

### What has been checked

Each packaged lossless codec is covered by a round-trip test on Linux:
encode 44,100 samples, finalize the file, decode it back, and compare
every sample (`tests/CUETools.Linux.Tests/NativeCodecTests.cs`). MP3 is
checked as far as a lossy codec allows, by finalizing the file and
finding a real MPEG frame in it. The three command-line encoders are
started as programs and have to report their pinned version
(`tests/CUETools.Linux.Tests/CliEncoderTests.cs`).

A real 24-track album was converted with the packaged codecs on
2026-08-13 and loaded back into Verify. The Monkey's Audio copy read
`accurate | confidence 29` and `verified | confidence 207`, the same two
panel readings its FLAC source earned the day before.

![The Verify and Repair page showing a verified album. The album verdict reads "Album verified" with the tally "1 database-confirmed" and counts of 1 disc, 24 tracks, 1:08:22. The disc chip reads DATABASE VERIFIED, the ACCURATERIP panel reads "accurate | confidence 29", the CUETOOLS DB panel reads "verified | confidence 207", and the source path at the top runs through a folder named "ape-out" before it is cut off. The first five track rows show AR conf values of 0/86, 0/88, 0/86, 0/87 and 0/86 with CTDB conf values of 224, 229, 230, 230 and 230.](2026-08-13-native-ape-verified.png)

*The Monkey's Audio copy, decoded and checked from scratch. The packaged
library wrote every track and read every track back. This screenshot is
from an older build, whose sidebar had no Rip page.*

## Related topics

- [Converting an album to another format](convert.md)
- [Ripping a CD to the format you choose](rip.md)
- [Checking an album against AccurateRip and CTDB](verify.md)
- [Converting or verifying a stack of albums in one go](queue.md)
- [Terms used in this manual](glossary.md)
