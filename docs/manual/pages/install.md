# Install and run

CUETools Linux is a desktop app for ripping CDs and for verifying,
repairing, and converting the rips you already have. This page covers what
your machine needs, how to install it, where it keeps things, and what it
sends over the network. Installing changes nothing in your music
collection. The app keeps its settings and logs under your home folder, it
writes a report next to an album when you verify one, and rips,
conversions, and repaired copies go where the page doing the work says
they go.

There is no published release yet, so the two packages below are the ones
you build from this project's own scripts. See
[Build the packages yourself](#build-the-packages-yourself).

## What your system needs

| Item | What to know |
| --- | --- |
| Processor | 64-bit x86 (amd64). No other architecture is built. |
| System C library | [glibc](glossary.md#glibc) 2.38 or newer. |
| Desktop | An X11 session, or a Wayland session running XWayland. This build ships only the X11 display backend. |
| Other libraries | `libx11-6`, `libice6`, `libsm6`, and `libfontconfig1`. The `.deb` pulls them in; the AppImage installs nothing, so they have to be there already. |
| Optical drive | Needed only for ripping. Your user has to be in the `cdrom` group. |

The glibc floor is the one requirement that rules distributions out. The
app binary, and two of the audio codec libraries packaged with it, use
symbols versioned `GLIBC_2.38`, so an older system refuses to load them.
That means Ubuntu 24.04 or later, Debian 13 or later, or a comparably
recent distribution. Ubuntu 22.04 and Debian 12 cannot run it today.

That floor is an accident of the machine the release was built on, not
something the app actually needs. Nothing in CUETools uses a feature that
arrived in glibc 2.38. The symbols involved are four ordinary,
long-standing ones (`fmod`, `fmodf`, `strtol`, `wcstol`) that a newer
compiler quietly binds to a newer version of itself. Building the release
on an older system lowers the floor without changing a line of code,
which is the fix being worked on.

### Optical drives need the cdrom group

CUETools finds optical drives by looking for `/sys/block/sr0`,
`/sys/block/sr1`, and so on, and labels them with letters the way the
Windows app does: drive A is `/dev/sr0`, drive B is `/dev/sr1`. Finding a
drive that way needs no permissions, but reading it does, so a drive you
are not allowed to open still appears in the drive list and then fails
every read. The Rip page reports `No disc in drive A:` whether or not a
disc is loaded.

Add yourself to the group once:

```console
sudo usermod -aG cdrom "$USER"
```

Then log out and back in, because group membership is applied at login.
Check that it took:

```console
id -nG | tr ' ' '\n' | grep -x cdrom
```

## Choose a package

Both packages carry the identical application, including the bundled audio
codecs and the command-line encoders. Choose by how you want it managed.

| Package | When to use it |
| --- | --- |
| `cuetools-linux_<version>_amd64.deb` | Debian, Ubuntu, and derivatives. Your package manager installs the app, the `cuetools-linux` command, the applications-menu entry, and the icon, and pulls in the four libraries above. |
| `CUEToolsLinux-<version>-x86_64.AppImage` | Any other distribution that meets the requirements. One file, nothing installed, no menu entry, and no dependency resolution. |

`<version>` is the app version, `0.1.0-alpha` in the current build. The
`.deb` writes it as `0.1.0~alpha`, with a tilde, which is how Debian sorts
a pre-release below the final version of the same number.

### Install the .deb

```console
sudo apt install ./cuetools-linux_0.1.0~alpha_amd64.deb
```

Keep the `./` in front of the file name. Without it, apt looks for a
package by that name in your configured repositories instead of installing
the file in front of you.

The package puts the application in `/usr/lib/cuetools-linux/`, a
`cuetools-linux` command in `/usr/bin/`, and, in
`/usr/share/doc/cuetools-linux/`, the license text for every third-party
binary it ships plus a written offer of the corresponding source.

### Run the AppImage

```console
chmod +x CUEToolsLinux-0.1.0-alpha-x86_64.AppImage
./CUEToolsLinux-0.1.0-alpha-x86_64.AppImage
```

The AppImage mounts its own contents with FUSE, so it needs a `fusermount`
program on your `PATH`. If it prints
`Error: No suitable fusermount binary found on the $PATH` or
`Cannot mount AppImage, please check your FUSE setup.`, run it this way
instead:

```console
./CUEToolsLinux-0.1.0-alpha-x86_64.AppImage --appimage-extract-and-run
```

That unpacks the contents to a temporary folder for the length of the run
and uses no FUSE at all. It starts more slowly and needs temporary disk
space, and it is otherwise the same application.

Every command-line option on this page works with the AppImage too: put it
after the file name.

### Build the packages yourself

Build first, then package. `eng/build.sh` stages the pinned engine, builds
the vendored codecs and the command-line encoders, runs the test suite,
and, with `--publish`, produces the binary the packaging scripts need.

```console
./eng/build.sh --publish
./eng/package-deb.sh
./eng/package-appimage.sh
```

Both packages land in `bin/packages/`. The scripts need the .NET SDK
pinned in `global.json` (10.0.100 or a later feature band), PowerShell
(`pwsh`), a C and C++ toolchain with `make`, plus `cmake`, `pkg-config`,
`patch`, `tar`, `unzip`, `binutils`, `curl`, and `python3`;
`package-deb.sh` also needs `dpkg-deb`, and `package-appimage.sh`
downloads a hash-pinned `appimagetool` on first use.

## Start CUETools

- From your applications menu: **CUETools Linux**.
- From a terminal: `cuetools-linux`, or the AppImage file.

The window opens on the **Verify & Repair** page in the dark theme, at
1200 by 720 pixels. The button in the top right reads **Light theme**;
pressing it switches the whole window immediately, and the label becomes
**Dark theme**. Your choice is saved and used the next time you start.

The rail on the left holds **Rip**, **Verify & Repair**, and **Convert**
under WORK, then **Queue** under SESSION. A fifth card,
**Enrichment pending**, with a count in brackets, appears only when
metadata lookups from an offline session are waiting for you to review
them.

![The CUETools Linux window with nothing loaded. The header reads CUETOOLS LINUX, with a Light theme button at the far right. The left rail lists, under WORK, "Verify & Repair / AccurateRip + CTDB" highlighted with a teal border and "Convert / Re-encode existing rips"; under SESSION it lists "Queue / Batch verify or convert" and "Enrichment pending (1) / Offline lookups ready to rev...". The page has an empty Source box with File... and Folder... buttons and a greyed-out Verify album button, and a large drop target reading "Drop an album folder here". The status line at the bottom reads "Drop an album folder, CUE sheet, playlist, or supported lossless file."](2026-08-13-enrich-pending-rail.png)

*The Verify & Repair page with nothing loaded, which is where a launch
lands. Two things differ from a first run on a current build: the rail
starts with Rip, which this older build did not have yet, and the
Enrichment pending card is absent until an offline session leaves lookups
waiting.*

Started from a terminal, the app prints one line when the window appears:

```text
startup-to-window-ms=<milliseconds>
```

That is how long the app took to put a window on screen. On the machine
this was measured on, a packaged build reports a little under 700, and an
empty drive keeps it there. Your own number depends on your hardware, so
treat it as something to compare against itself over time rather than
against this one: a launch that suddenly takes several times what it
usually does is worth looking into.

If an audio CD is in the drive when CUETools starts, the Rip page reads it
straight away, and reading a disc includes database lookups. See
[What leaves your machine](#what-leaves-your-machine).

Closing the window saves your settings. So does a Ctrl+C in the terminal,
and so does your session manager stopping the app when you log out.

## Run it from a terminal

A path is an album folder, a CUE sheet, a playlist (`.m3u` or `.m3u8`), or
a supported lossless audio file. More than one path is allowed wherever
`<path>...` appears.

| Command | What it does |
| --- | --- |
| `cuetools-linux` | Opens the window on the Verify & Repair page. |
| `cuetools-linux <path>...` | Loads those albums on the Verify & Repair page and stops there. |
| `cuetools-linux --verify <path>...` | Loads them and starts the verification. |
| `cuetools-linux --repair <path>...` | Loads them, verifies, and repairs every repairable disc without stopping to ask. The flag is your consent, given up front. |
| `cuetools-linux --convert <path> [--convert-to <format>] [--convert-out <dir>]` | Opens the Convert page and converts that album. `<format>` is a format name from the Convert page's list; `<dir>` is the output folder, and without it the page's usual output location applies. |
| `cuetools-linux --queue <path>... [--queue-run]` | Adds every path to the Queue page under the current action defaults. `--queue-run` also starts the batch. |
| `cuetools-linux --enrich <path>` | Looks the album up and applies the metadata proposal without showing the approval dialog. The flag is your consent, given up front. |
| `cuetools-linux --rip-page [--drive <letter>]` | Opens on the Rip page. `--drive` selects that drive if it is attached. |
| `cuetools-linux --smoke` | Opens the window, prints the startup line, and exits. Useful for checking that an install runs at all. `--smoke` has to be the first argument. |

## What the app writes, and where

Everything here stays on your machine.

| What | Where |
| --- | --- |
| Theme preference | `~/.config/cuetools-linux/theme.txt` |
| Settings | `~/.config/CUETools2026/settings.txt` |
| Diagnostic logs | `~/.config/CUETools2026/logs/` |
| Recently ripped list | `~/.config/CUETools2026/history.json` |
| Drive calibration | `~/.config/CUETools2026/drive-calibration.json` |
| Rip verification history | `~/.config/CUETools2026/verify-history.json.gz` |
| AccurateRip drive offset table | `~/.config/CUE Tools/AccurateRipCache/DriveOffsets.bin` |
| Offline backfill queue | `~/.local/state/cuetools-linux/journal/` |
| Drive lease locks | `~/.local/share/CUETools2026/drive-leases/` (only when you have an optical drive) |
| Verify reports | Next to the verified album (`<name>.accurip` and `<name>.toc`, where `<name>` is the CUE sheet, playlist, or audio file you verified) |
| Rips and conversions | `~/Music/CUETools` unless you choose another folder on the page |

Settings are read once at startup and written back when the app exits, so
editing `settings.txt` while CUETools is running has no effect: the file
is overwritten on the way out.

Each launch writes one new diagnostic log file, named for the date, time,
and process. Nothing prunes them, so they accumulate until you delete
them. They record what the app did (phases, counts, confidences, timings,
and full error detail) with your user name, your home folder path, and the
current album's artist and title scrubbed out of every line.

The drive offset table is a copy of AccurateRip's published list of how
far each drive model reads off centre. CUETools re-downloads it when the
cached copy is more than ten days old.

## What leaves your machine

### Verifying or repairing an album

Before each verification, CUETools opens a TCP connection to
`db.cuetools.net` on port 80 and to `www.accuraterip.com` on port 443, to
work out whether it is online. It stops at the first one that answers,
and gives each three seconds.

[AccurateRip](glossary.md#accuraterip) is asked over HTTPS at
`https://www.accuraterip.com/accuraterip/`. The disc's identity is in the
address itself, in the form of three ids computed from its track layout.

[CTDB](glossary.md#ctdb-cuetools-database) is asked at
`http://db.cuetools.net/lookup2.php`, with the disc's
[track layout](glossary.md#toc-id-table-of-contents-id) in the query
string. That is plain HTTP, not HTTPS, because the CTDB server does not
answer TLS, so the request and its answer are readable in transit. The
request identifies the software by name and version, and includes your
kernel's version string.

A repair also downloads recovery data, either from `db.cuetools.net` or
from a mirror address that the lookup response names.

### Reading a CD

Reading a disc on the Rip page, or at startup when a disc is already in
the drive, sends more than a verification does.

- The drive offset table is fetched from
  `https://www.accuraterip.com/accuraterip/DriveOffsets.bin` when the
  cached copy is missing or over ten days old.
- CTDB is asked for the disc's metadata, at the same address and over the
  same plain HTTP as above. During a rip the User-Agent line also carries
  your drive's model name.
- If CTDB returns no metadata for the disc, freedb is asked at
  `http://gnudb.gnudb.org/~cddb/cddb.cgi`, also over plain HTTP.
- Once the disc has an album or artist name, cover art is looked up
  without being asked for: `https://musicbrainz.org/ws/2/` is queried by
  disc id, then `https://coverartarchive.org/`, which serves its images
  from `archive.org`. The first eligible front cover is downloaded and
  used.
- Artwork that CTDB itself returned with the metadata is fetched from
  whatever HTTPS address CTDB gives for it, which need not be either of
  those two hosts.

### Previewing metadata with Enrich

Enrich uses the same CTDB metadata lookup, and the same freedb fallback.
A proposed cover image is downloaded only from `coverartarchive.org`,
`archive.org`, or `db.cuetools.net`, and only over HTTPS.

### Sharing a rip with CTDB, if you say yes

After a clean rip or a clean verification, CUETools may offer to share the
result with the CUETools Database, so other people can check their own
copies of the same disc against yours. It asks with a dialog, once; a
"remember" checkbox stops the asking, in either direction. Nothing is ever
sent before you answer yes.

The dialog itself lists what sharing sends, and this page repeats it: the
disc's track layout, a checksum for each track, recovery (parity) data,
the artist and album title, the barcode if the disc has one, and an
identifier for this computer that the database uses to tell separate
submissions apart. The upload goes to `db.cuetools.net` over the same
plain HTTP as the lookups, so it is readable in transit.

It does not send your audio files, your file names, or anything about
where your music is stored. Sharing cannot be undone: the database has no
delete. Only a rip or verification with no unrecoverable errors is ever
offered; a salvaged or partly-failed read cannot be shared at all, even
with a remembered yes.

After you answer, the page's status line says whether the upload actually
landed or failed. If you said yes and see no such line, treat it as not
sent.

Your remembered answer is stored in the settings file. Until the settings
screen arrives, changing your mind means editing
`~/.config/CUETools2026/settings.txt` with the app closed: `CTDBSubmit`
is the answer, and `CTDBAsk` set back to `1` makes the dialog ask again.

### What never goes out

- Nothing is submitted to AccurateRip. There is no submission path to it
  in this build at all.
- Nothing is submitted to CTDB without a yes to the sharing dialog above.
- Converting an album, running a conversion queue, and reading files from
  disk send nothing.
- There is no telemetry and no update check. The app makes no network
  request that is not one of the lookups above or a sharing upload you
  agreed to.

## If something goes wrong

### The app exits immediately on Ubuntu 22.04 or Debian 12

Those releases ship a glibc older than 2.38, and the app needs symbols
from 2.38, so the system's loader refuses to start it. There is no flag
or workaround for this in the current build, because the loader makes
that decision before any of the app's own code runs. Run it on Ubuntu
24.04, Debian 13, or a comparably recent distribution. Nothing about the
app genuinely requires that version (see
[What your system needs](#what-your-system-needs)), so the floor should
drop in a later release.

### The AppImage will not start and mentions FUSE

The AppImage runtime mounts the image with FUSE and cannot find a
`fusermount` program on your `PATH`. Run it with
`--appimage-extract-and-run`, which unpacks to a temporary folder instead
and needs no FUSE. See [Run the AppImage](#run-the-appimage).

### The AppImage does not appear in the applications menu

That is expected. The AppImage installs nothing and registers nothing;
run it from a file manager or a terminal. Install the `.deb` if you want
a menu entry and an icon.

### The Rip page says "No optical drive found."

CUETools lists drives by looking for `sr` devices under `/sys/block`, so
this message means the kernel is not presenting one. Check with:

```console
ls /sys/block | grep '^sr'
```

If that prints nothing, the drive is not attached or not detected, and it
is not a CUETools problem. If it prints `sr0`, restart CUETools.

### The Rip page says there is no disc, but a disc is loaded

The usual cause is permission on the drive. CUETools lists drives from
`/sys/block`, which needs no permissions, and then opens the drive's
device node to read it, which does. When that open fails the app cannot
tell it apart from an empty drive, so it reports `No disc in drive A:`.
Check your group membership as described in
[Optical drives need the cdrom group](#optical-drives-need-the-cdrom-group).

If a disc is loaded and the message names a disc type instead, for
example `Not an audio CD - DVD-ROM in drive A:`, the drive read fine and
the disc is not an audio CD.

### A verification says it is offline while your connection works

CUETools decides it is online by opening a plain TCP connection to
`db.cuetools.net` on port 80 and to `www.accuraterip.com` on port 443,
and calls itself offline only when neither answers within three seconds.
That probe does not use the proxy settings in the profile, so a network
where all outbound traffic has to go through a proxy reports offline even
though the rest of your machine works. The verification still completes
locally and is queued; see
[offline behavior and backfill](offline-and-backfill.md).

## Uninstall

```console
sudo apt remove cuetools-linux
```

For the AppImage, delete the file.

Neither removes anything listed in
[What the app writes, and where](#what-the-app-writes-and-where). Those
paths stay until you delete them yourself. Deleting them loses your
settings, your drive calibration, and any verifications still queued for
[backfill](glossary.md#backfill); your music and your verify reports are
untouched, because they live with your albums.

## Related topics

- [Verify an album against AccurateRip and CTDB](verify.md)
- [What happens when you verify offline, and how backfill catches up](offline-and-backfill.md)
- [Which audio formats this build reads and writes](codecs.md)
- [What the app stores, and what you can change](settings.md)
- [Terms used in this manual](glossary.md)
