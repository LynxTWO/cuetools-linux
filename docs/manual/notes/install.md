# Install and run

## Installing

- **.deb (Ubuntu and derivatives):** `sudo apt install ./cuetools-linux_<version>_amd64.deb`
  installs the app, the `cuetools-linux` command, the desktop entry, and
  the icon. Dependencies are four small X11 libraries every desktop
  already has.
- **AppImage (any distro):** download, `chmod +x`, run. One file, no
  install. Needs FUSE (`libfuse2` package family) like all AppImages.

Both packages carry the identical application; measured at 17 MB (.deb)
and 20 MB (AppImage), about 49 MB installed.

## Launching

- From the app grid: "CUETools Linux".
- From a terminal: `cuetools-linux`, optionally with an album path;
  `--verify` starts verification immediately.
- The app opens to the Verify page in the dark theme in well under two
  seconds; the header's theme button switches dark/light live and the
  choice persists.

## Where the app keeps things

Everything is local and user-owned; nothing phones home.

| What | Where |
| --- | --- |
| Theme preference | `~/.config/cuetools-linux/theme.txt` |
| Backfill journal | `~/.local/state/cuetools-linux/journal/` |
| Diagnostic logs (structural only, no album or artist names) | `~/.config/CUETools2026/logs/` |
| Verify reports | next to the verified album (`<name>.accurip`) |

## Uninstalling

`sudo apt remove cuetools-linux` (or delete the AppImage). User data
above remains until you delete it; the app never deletes anything on its
own.
