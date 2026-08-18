# Settings (what persists, and where)

## The settings screen shipped 2026-08-18 (D-073)

This section previously opened "There is no settings screen yet". There
is now: a rail page under SESSION, immediate apply with the existing
save-on-exit contract untouched, a named Privacy & data group holding
both remembered consents and log retention, and no Settings entry at all
in secondary drive windows. The interview record is D-073 in
DECISION-LOG.md; the page content is documented in pages/settings.md.

What the work pages put in front of you is unchanged: output folder,
format and codec, correction quality, and the layout choice stay where
the work happens.

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
