# Settings and where files live

CUETools Linux keeps its options in two places: a **Settings** page in
the rail, and the controls on the pages that use them. Everything is
saved for you: the app reads one settings file when it starts, and writes
that file back when it exits. This page is the reference for the Settings
page, for what survives a restart, for the few things only a text edit
can change, and for where the app keeps its own files. All of them are
under your home folder, and none of them are inside your music.

Because the app rewrites the settings file on its way out, edit that file
with CUETools closed. An edit made while the app is running is
overwritten when it exits.

## The Settings page

**Settings** sits at the bottom of the rail, under SESSION. Every change
applies the moment you make it, the way the theme button always has;
there is no Apply or OK, and the file is written when the app exits. A
job that is already running keeps the options it started with, so a
mid-rip change steers the next job, never the one in flight.

Its sections: Ripping, AccurateRip & CTDB, File naming & output, Gaps &
HTOA, Tagging, Album art, Privacy & data, HDCD, Encoders, and
Diagnostics. Two are worth introductions:

**Privacy & data** holds the answers the app once asked for and then
remembered, so a changed mind no longer means editing a file:

- **Look up cover art automatically** is the first-run artwork question
  as a toggle. Changing it here counts as answering, so the first-run
  dialog will not ask someone who has already decided.
- **Share verified rips with CTDB** is the sharing question as one
  choice: *Ask before sharing*, *Always share*, or *Never share*.
  Choosing *Ask before sharing* re-arms the dialog. Whatever you choose,
  a damaged, salvaged, or held rip is never shared:
  [what sharing sends](install.md#sharing-a-rip-with-ctdb-if-you-say-yes)
  covers the payload, and the quality rule cannot be overridden.
- **Keep logs forever** turns off log deletion for archiving. It ships
  off, and the retention rule it replaces is written beside it: logs
  older than 90 days are removed once there are more than 200, whichever
  rule keeps more.

**Encoders** lists the optional command-line encoders the app cannot
bundle, with a **Download** link to each official project and a
**Locate...** that imports your downloaded copy. An import that is
refused says why, in red, under the encoder's status line.

**Prevent sleep during a rip** holds a system sleep inhibitor for
exactly as long as a rip or verify runs, the same request a video
player makes during playback, so a long secure rip is not cut short by
an idle timeout. The rip log says when the hold begins; if the machine
cannot grant one (no systemd, for instance), the log says
`the system may sleep during this rip` instead of pretending. The
screen may still blank - only sleep is held off, and a crashed app
releases the hold by itself.

A window CUETools opened for a second drive has no Settings entry at
all. Only the main window owns the saved profile, so the page lives
only there.

## What you can change from the work pages

The rail on the left holds **Rip**, **Verify & Repair**, **Convert**, and
**Queue**, with **Settings** below them. The only control in the window
frame itself is the theme button in the top right.

| Control | Where it is | Kept for next time |
| --- | --- | --- |
| **Light theme** / **Dark theme** | Header, top right | Yes, written the moment you press it |
| Output [codec](glossary.md#codec), in the **Choose output codec** window | Rip page | Yes |
| Output [codec](glossary.md#codec), in the **Choose output codec** window | Convert and Queue pages | Partly. The encoder chosen for a format is kept, but which format those pages start on is not. |
| Compression, quality, and the codec's advanced options, under **tune** | Convert page only | Yes, saved against that implementation |
| The output folder box and **...** | Rip page | Yes, for the Rip page |
| **cue** and **log** | Rip page | Yes |
| **quality** (`Burst`, `Secure`, `Paranoid`, `Salvage`) | Rip page | Yes |
| **layout** (`Tracks`, `Image + embedded CUE`) | Rip page | Yes |
| **Choose folder...**, the conversion output folder | Convert page | No. Every launch starts at `~/Music/CUETools` again |

The output folder on the Rip page belongs to the Rip page. The Queue
page has an output row of its own for conversions: each queued item
freezes the folder that was chosen when it was added, so changing the row
steers what is queued next and never moves work already waiting. Left at
its default, each album's converted files go to its own usual location.
Neither the Convert nor the Queue folder survives a restart.

Everything else keeps the value it was built with until you edit the
settings file. [Settings with no control](#settings-with-no-control)
covers the ones worth knowing about.

## When your settings are written

- The settings file is read once, at startup, before any page is built.
  Nothing re-reads it while the app runs.
- It is written back when the app exits normally: closing the window,
  pressing Ctrl+C in the terminal you started it from, or your session
  manager stopping the app when you log out.
- It is also written when you press **Rip**, **Verify only**, or
  **Test & Copy** on the Rip page, so a window opened for a second drive
  during a long job starts from the same saved choices.
- The first run has no file. The built-in defaults apply, and the first
  exit creates the file.
- A crash, or `kill -9`, skips the save, and that session's changes are
  lost.
- A window CUETools opened for another drive never writes the file. Its
  title bar names the drive, as in `CUETools Linux - Drive B`.

The file is replaced whole rather than edited in place: CUETools writes a
temporary file beside it and renames that into position, so an
interrupted save cannot leave you with half a file. The empty
`settings.txt.lock` file beside it stops two windows replacing the file
at the same moment. A window that finds the lock already held does not
wait for it: it gives up at once, records `settings save failed:
IOException` in its log, and that session's changes are not written.

## Where the app keeps its files

| What | Where |
| --- | --- |
| Settings | `~/.config/CUETools2026/settings.txt` |
| Theme preference | `~/.config/cuetools-linux/theme.txt`, holding the single word `Dark` or `Light` |
| Diagnostic logs | `~/.config/CUETools2026/logs/`, one file per launch |
| Recently ripped list | `~/.config/CUETools2026/history.json` |
| [Drive calibration](glossary.md#drive-calibration) | `~/.config/CUETools2026/drive-calibration.json` |
| Your own rip verification history | `~/.config/CUETools2026/verify-history.json.gz` |
| AccurateRip drive offset table | `~/.config/CUE Tools/AccurateRipCache/DriveOffsets.bin` |
| Discs queued for [backfill](glossary.md#backfill) | `~/.local/state/cuetools-linux/journal/`, one small file per entry |
| Drive claim files | `~/.local/share/CUETools2026/drive-leases/` |

Your music is not in that list, and neither is anything the app produces
for you. Verify reports are written next to the album they describe, and
rips, conversions, and repaired copies go where the page doing the work
says they go.

Several of these files have an empty `.lock` file beside them, such as
`settings.txt.lock` and `history.json.lock`. They hold no data. They
exist so that two CUETools windows cannot write the same file at the same
moment, and they stay behind when the app closes. Leave them alone.

Removing the app leaves all of this in place; see
[uninstall](install.md#uninstall).

## Inside settings.txt

The file is plain text, UTF-8, several hundred lines of `key=value`. A
few rules decide what your edit does:

- A boolean key takes `1` or `0`. Any other text counts as "not set", and
  the built-in default applies instead.
- `WpfCorrectionQuality` takes `0` (Burst), `1` (Secure), `2` (Paranoid),
  or `3` (Salvage). Anything else falls back to `1`.
- `WpfRipOutputLayout` takes `Tracks` or `ImageWithEmbeddedCue`, spelled
  with those capitals, or the numbers `0` and `1` that stand for them.
  Anything else falls back to `Tracks`.
- If a key appears twice, the first one wins.
- The block that starts `Advanced={` runs for a couple of hundred lines,
  each continuation line starting with `=`. It holds every encoder and
  decoder definition, which is where the **tune** window's choices end
  up.
- The app writes back only the keys it knows, so anything else you add to
  the file is gone after the next exit. The format has no comments.

Keys beginning with `Wpf` are the app's own settings rather than the
engine's. The prefix is historical: this is the same settings file the
Windows CUETools 2026 app writes, in the Windows equivalent of this
folder, and a file copied from there loads here apart from the
credentials described in
[Passwords and API keys](#passwords-and-api-keys-are-not-stored).

Two failures are worth recognising, and both leave a line in the newest
file under `~/.config/CUETools2026/logs/`:

- If the `Advanced={` block cannot be read, CUETools keeps its built-in
  values for that block, loads the rest of the file, and logs
  `advanced settings were rejected; previous/default values retained`.
- If the file cannot be read at all, the app still starts, on defaults
  throughout, and logs `settings load failed - using defaults:` with the
  reason. The next exit replaces the file, so move your copy aside first
  if you want to salvage it.

## Settings with no control

Most of what used to be in this table moved onto the Settings page:
stop-on-unrecoverable, deep recovery, adaptive read speed, and the tray
lock are all in its Ripping section now, described where the switch is.
What remains file-only, changed in `~/.config/CUETools2026/settings.txt`
with the app closed:

| Key | Default | What it does |
| --- | --- | --- |
| `WpfNamingTemplate`, and the five `WpfNaming...` keys | see below | The folder and file names rips and conversions are written under. The template has no editor in this build; the five clean-up switches are described below. |

The [Rip page](rip.md) describes what the read modes do; deep recovery
and adaptive read speed are covered there in
[how it works](rip.md#how-it-works).

One near-miss is worth naming so nobody hunts for it: the Settings page's
**Track filename template** row is not `WpfNamingTemplate`. It is the
engine's own template, and it names the tracks of a conversion written in
tracks mode. Rips are named by `WpfNamingTemplate` below, whatever the
engine template says.

## How rips and conversions are named

The Rip page, the Convert page, and the Queue page all build their output
paths from one template, `WpfNamingTemplate`. It ships as:

```text
%albumartist% - %album%[%releasedescriptor%]/[%disc%]%tracknumber% - %title%[%featsuffix%]
```

The `/` divides the folder part from the file name. Square brackets mark
a piece that disappears when it is empty, which is how a single-disc
release avoids an empty `Disc` folder. So a single-disc album whose album
artist is "The Beatles" lands in a folder like
`Beatles, The - Abbey Road (1969)`, with its first track written as
`01 - Come Together.flac`.

Five switches clean up the values that go into it. All five ship as `1`.

| Key | With `1` |
| --- | --- |
| `WpfNamingExtractFeatured` | A guest credit in the track artist moves to the end of the file name, as `(feat. Guest)`. |
| `WpfNamingUnifySeparators` | Separators between collaborating artists become `&`. The words and symbols rewritten that way are `meets`, `vs`, `vs.`, `with`, `and`, `x` (upper or lower case), `+`, `;`, `\|`, and the multiplication, bullet, and middle-dot symbols. |
| `WpfNamingHandleArticles` | A leading article moves to the end of an artist name, turning "The Beatles" into "Beatles, The" so folders sort by the name people look for. Titles are left alone. |
| `WpfNamingStripIllegal` | `:` becomes a spaced hyphen, and `*`, `?`, `<`, `>`, and `\|` are removed. |
| `WpfNamingReleaseDescriptor` | The album folder carries a bracketed descriptor. In this build that is the year, plus `[2-CD Set]` (or `[3-CD Set]`, and so on) for a multi-disc set. The engine can also write `[EP]`, `[Single]`, and `[Promo]` when a release says so. |

Three clean-ups are not optional, because they are about where files land
rather than how they read. A `/` or a `\` inside an artist or title
becomes `-`, so a name like "AC/DC" cannot invent a folder; a double
quote is always removed; and any character your filesystem rejects is
dropped whatever the five switches say.

## Passwords and API keys are not stored

CUETools Linux never writes a password or an API key into the settings
file. The Windows app protects both with Windows' own per-user
encryption, this app has no equivalent wired up, and plain text in the
file is not offered as the fallback.

Nothing in this window asks you for either one, so this matters in a
single case: a settings file copied from a Windows machine. Its protected
proxy password and its TheAudioDB key are treated as "not set" here, and
the next exit rewrites the file without them. The log records `protected
proxy credential unavailable; clear and set it again` for the proxy
password, and a matching line for the TheAudioDB key. Keep a copy of
the Windows file if you still need it there.

One consequence is worth naming. TheAudioDB is an optional extra source
of cover art, and it needs an API key, so it cannot be switched on in
this build at all. Setting `WpfTheAudioDbEnabled=1` by hand changes
nothing, because the key it depends on is never loaded.

The engine behind the app also understands two CTDB submission settings,
`CTDBSubmit` and `CTDBAsk`. These are the sharing question's memory: the
first clean rip or verification asks whether to share the result with the
CUETools Database, and ticking "remember" writes your answer to
`CTDBSubmit` and stops the asking by setting `CTDBAsk` to `0`. To be asked
again, set `CTDBAsk` back to `1` with the app closed. Nothing is ever
shared without a yes, and a rip with unrecoverable errors is never shared
at all. See
[sharing a rip with CTDB](install.md#sharing-a-rip-with-ctdb-if-you-say-yes).

## If something goes wrong

### Your edit to settings.txt is gone

CUETools was running when you saved it. The app holds its settings in
memory from startup and writes the whole file back when it exits, which
replaces whatever you wrote. Close CUETools, edit the file, then start it
again.

### The app started with all its settings back at their defaults

The file could not be read. The newest log under
`~/.config/CUETools2026/logs/` carries the reason on a line starting
`settings load failed - using defaults:`. You can read the settings lines
straight out of the logs:

```console
grep " settings " ~/.config/CUETools2026/logs/*.log | tail -20
```

The next exit overwrites the unreadable file with defaults, so copy it
somewhere else first if you want to recover anything from it.

### The Rip page shows ~/Music/CUETools although the file names another folder

The saved folder no longer exists. The Rip page falls back to
`~/Music/CUETools` when the folder it saved has gone, rather than failing
a rip on a path that is not there. Press **...** and pick a folder that
exists; that choice is saved on exit.

### One window's changes did not stick

Only the window you started first writes the settings file. A window
CUETools opened for a second drive, titled `CUETools Linux - Drive B`,
deliberately never publishes settings, so a change made there lasts until
that window closes. Make the change in the first window.

## Related topics

- [Installing CUETools, and what it writes to your machine](install.md)
- [Ripping a CD, and the read modes the quality box selects](rip.md)
- [Which audio formats this build reads and writes](codecs.md)
- [What happens when you verify offline, and how backfill catches up](offline-and-backfill.md)
- [Terms used in this manual](glossary.md)
