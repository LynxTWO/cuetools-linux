# Install and run

## What your system needs

- **64-bit x86 Linux** (amd64). No other architecture is built yet.
- **A recent glibc.** The current binary needs glibc 2.38 or newer, which
  means Ubuntu 24.04 or later, Debian 13 or later, or a comparably recent
  distribution. Ubuntu 22.04 and Debian 12 cannot run it today. This is a
  known limitation, not the intended floor, and it is being worked on.

  Measured 2026-08-17 with `objdump -T` over every shipped native binary
  in `src/CUETools.Linux.App/bin/Release/net10.0/linux-x64/publish`.
  Exactly three of the seven reach 2.38, so the manual page's "the app
  binary, and two of the audio codec libraries" is accurate:

  | Binary | Highest GLIBC version needed |
  | --- | --- |
  | `CUETools.Linux.App` (AOT apphost) | 2.38 |
  | `libmp3lame.so` | 2.38 |
  | `MACLibDll.so` | 2.38 |
  | `libFLAC_dynamic.so` | 2.34 |
  | `wavpackdll.so` | 2.34 |
  | `libSkiaSharp.so` | 2.27 |
  | `libHarfBuzzSharp.so` | 2.14 |

  Every 2.38 reference is a build-toolchain artifact, not a feature the
  code uses. The apphost needs only `fmod` and `fmodf`, which glibc 2.38
  re-versioned; the older `fmod@GLIBC_2.2.5` is still present on new
  systems, so linking on an older one picks it up instead. The two codec
  libraries need only `__isoc23_strtol` and `__isoc23_wcstol`, which are
  what glibc 2.38's headers redirect plain `strtol`/`wcstol` to when a
  C23-aware compiler (GCC 13+) builds them. Those variants differ from the
  originals only in parsing `0b` binary literals, which neither library
  does.

  So the floor is set by the build machine, not by the source. Building
  the release under an older glibc (a container is the usual way) should
  lower it to 2.34 with no code change. Dropping the two codecs would not
  help, because the apphost reaches 2.38 on its own. Not yet attempted, so
  the requirement stands as written. See findings F-31.
- **A desktop with X11 or Wayland**, plus the small X11 client libraries
  every desktop already installs.
- **Membership of the `cdrom` group** to use an optical drive. Without it
  the drive is visible but unreadable, and the app cannot tell that apart
  from an empty drive. Add yourself once:

  ```
  sudo usermod -aG cdrom "$USER"
  ```

  Then log out and back in, because group membership is applied at login.
  Check it took with `id -nG | tr ' ' '\n' | grep -x cdrom`.

## Installing

- **.deb (Debian, Ubuntu, and derivatives):**
  `sudo apt install ./cuetools-linux_<version>_amd64.deb` installs the app,
  the `cuetools-linux` command, the desktop entry, and the icon.
- **AppImage (other distributions):** download, `chmod +x`, run. One file, no
  install. It needs a `fusermount` program on the PATH. If FUSE is unavailable
  or you would rather not install it, run the AppImage with
  `--appimage-extract-and-run`.

  Do not name `libfuse2` as the requirement. Tested 2026-08-17 on this
  workstation, where neither `libfuse2` (no such package on Ubuntu 24.04) nor
  `libfuse2t64` is installed: the shipped AppImage mounted anyway, because
  `fuse3` supplies `/usr/bin/fusermount` as a symlink to `fusermount3`. Proved
  by `--appimage-mount`, which reported `/tmp/.mount_CUETooihBKKM` while
  `mount` showed it as `type fuse.CUEToolsLinux-0.1.0-alpha-x86_64.AppImage`,
  so it really was a FUSE mount and not a silent extract-and-run fallback.
  The requirement is the `fusermount` binary, which several different packages
  can supply, and `pages/install.md` already words it that way.

Both packages carry the identical application, including the bundled audio
codecs and command-line encoders. Current sizes are printed by the packaging
scripts and stated in each release's notes; treat the release notes as the
authority rather than any number written here.

## Launching

- From the app grid: "CUETools Linux".
- From a terminal: `cuetools-linux`, optionally with an album path;
  `--verify` starts verification immediately.
- The app opens to the Verify page in the dark theme in well under two
  seconds; the header's theme button switches dark and light live, and the
  choice persists.

## Where the app keeps things

Everything the app writes stays on your machine and belongs to you.

| What | Where |
| --- | --- |
| Theme preference | `~/.config/cuetools-linux/theme.txt` |
| Application settings | `~/.config/CUETools2026/settings.txt` |
| Backfill journal | `~/.local/state/cuetools-linux/journal/` |
| Diagnostic logs (structural only, no album or artist names) | `~/.config/CUETools2026/logs/` |
| Verify reports | next to the verified album (`<name>.accurip`) |

## What leaves your machine

Verification and repair are online services, so some data does go out:

- **AccurateRip** lookups, over HTTPS, carry the disc's identifiers.
- **The CUETools Database** carries the disc's table of contents and rip
  evidence. These requests are not encrypted in transit today, because the
  service does not answer TLS.
- **Reading a disc** identifies it: its table of contents goes to the CUETools
  Database and to MusicBrainz to get the artist, album, and track titles. That
  is what fills the Rip page in, and it happens as soon as a disc is read.
- **Cover art** is fetched only after you answer yes to the question CUETools
  asks the first time it could look one up. Answer no and it never fetches one
  by itself; opening the artwork browser still fetches, because that is asking.
- **Database submissions**: SLICE-012 shipped 2026-08-18, so this entry now
  says what it sends. After a clean rip or verify the app asks once, with a
  dialog whose text lists the payload; a remembered answer stops the asking
  (D-069). A yes uploads the disc's table of contents, per-track checksums,
  Reed-Solomon parity, the artist and album title, the barcode, and the
  per-machine identifier the CTDB client sends to tell submissions apart, to
  db.cuetools.net over the same plain HTTP as the lookups. D-070 gates
  quality: any unrecoverable window, salvage, or held result is refused
  before the dialog ever shows, and a remembered yes cannot override that.
  Nothing is sent on no, and nothing is sent before an answer.

  Live evidence, 2026-08-18: one submission of a clean Test and Copy rip
  (AR 205/216). The diagnostic log recorded the server's reply, "TOCID has
  been confirmed", with album and artist redacted. An independent raw HTTP
  lookup afterwards showed the matching entry's confidence at 797, up from
  796 before the click, total 821 from 820, with both minor variant
  pressings unchanged. S12-002 closed.

Converting, repairing, and reading your own files send nothing.

Corrected 2026-08-16 and 2026-08-17. This section previously said that reading
a disc sends nothing, which was never true of the identification lookup, and
described a submission identifier for submissions that never happened in any
modern head until SLICE-012. See needs-verification.md entries 3 and 16, and
F-23 in docs/FINDINGS-2026-08-16-manual-pass.md.

## Uninstalling

`sudo apt remove cuetools-linux`, or delete the AppImage. The user data above
remains until you delete it; the app never deletes anything on its own.
