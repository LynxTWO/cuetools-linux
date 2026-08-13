# Settings (what persists, and where)

## The contract, in one paragraph

Everything you configure survives a restart: output folders, naming
scheme and its switches, format and codec selections, correction
quality, and every engine option. Settings load once at startup and save
on every graceful exit - including when your session manager stops the
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
