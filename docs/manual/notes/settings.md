# Settings (what persists, and where)

## There is no settings screen yet

This page describes what the app stores, not what you can change from the
interface. There is no settings page today. What you can reach is what the
Verify, Convert, and Rip pages put in front of you: output folder, format
and codec, correction quality, and the layout choice.

Everything else keeps its default, and two of those defaults are worth
knowing about:

- **Output naming** follows a fixed template, so rips and conversions land
  in a folder structure you did not choose.
- **Stop on unrecoverable** is off, so a badly damaged disc finishes with
  unrecovered samples in the output rather than stopping.

Both can only be changed today by editing
`~/.config/CUETools2026/settings.txt` while the app is closed, because the
app rewrites that file when it exits. A settings page is planned before the
first public release.

## The contract, in one paragraph

Everything the app does configure survives a restart: output folders, naming
scheme and its switches, format and codec selections, correction quality,
and the engine options the pages expose. Settings load once at startup and
save on every graceful exit - including when your session manager stops the
app (SIGTERM) or you press Ctrl+C in a terminal.

## Where they live

`~/.config/CUETools2026/settings.txt` - the same classic key=value
profile format the Windows CUETools 2026 app writes. Where fields
overlap, a profile written by one app loads in the other.

## Credentials are the exception, on purpose

Proxy passwords and API keys are never written to the Linux profile.
The Windows app protects them with per-user encryption (DPAPI); Linux
has no equivalent wired up yet, and storing them in plain text is not an
option we offer. If a profile carries a Windows-protected credential,
this app treats it as "no credential set" rather than guessing.
