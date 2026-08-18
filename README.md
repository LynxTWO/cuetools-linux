# CUETools Linux

A native Linux desktop app bringing the CUETools 2026 experience to Linux:
rip CDs with calibrated assurance, verify rips against AccurateRip and the
CUETools Database, repair damaged rips from CTDB parity, and convert between
lossless formats. Same engine as the Windows build, same evidence discipline,
in a package a fraction of the size.

**Status: built, unreleased, and not yet recommended for strangers.** Twelve
slices are complete with evidence, from verification through ripping to
database contribution. There is no published release yet, and several things
a first-time user needs are still missing. See
[Known limitations](#known-limitations) before trying it.

## What works today

| Capability | State |
| --- | --- |
| Verify a rip against AccurateRip and CTDB | Built, evidenced |
| Repair a damaged rip from CTDB parity | Built, evidenced on a real scratched disc |
| Convert between lossless formats | Built, evidenced across FLAC, ALAC, APE, WavPack |
| Batch queue for verify and convert | Built, evidenced |
| Rip a CD, including secure and Test and Copy modes | Built, evidenced on three drives |
| Metadata and cover-art enrichment | Built, evidenced |
| Offline journal and automatic backfill | Built, evidenced |
| Share a verified rip with the CUETools Database, by consent | Built, one live submission confirmed by independent lookup |
| Settings page, including privacy and consent controls | Built, evidenced |

Every claim above traces to a slice brief in `docs/` with its own evidence
section. Nothing here is inferred from a screenshot.

## Known limitations

Read these before installing. They are the reason there is no release yet.

- The current binary needs glibc 2.38 or newer, so it does not start on
  Ubuntu 22.04 or Debian 12 despite those being the intended floor. The
  floor is a build-machine artifact, not a code requirement; the fix is a
  build-environment change. See F-38 in the findings.
- A wedged USB drive still needs guidance the app does not give yet:
  SLICE-011 (guided drive recovery) is the gate the first public preview
  waits for (D-065).
- The output naming template has no editor; changing it means editing
  `~/.config/CUETools2026/settings.txt` with the app closed.
- Keep-awake is not wired up on Linux, so the machine may sleep during a
  long rip. The switch exists on the Settings page and says so.

## The documents

| File | What it is |
| --- | --- |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | The puzzle: modules, interfaces, data flow, technology, extension points |
| [docs/ENGINEERING.md](docs/ENGINEERING.md) | The rules: requirements, data model, security, standards, verification, definition of done |
| [docs/DECISION-LOG.md](docs/DECISION-LOG.md) | Every significant choice, its reasoning, and its revisit trigger (D-001 onward) |
| [docs/SLICE-001-verify.md](docs/SLICE-001-verify.md) | The current build boundary: album verification, end to end |
| [docs/TRIAGE.md](docs/TRIAGE.md) | Project triage card and unknowns list |
| [docs/RESEARCH-2026-08-11.md](docs/RESEARCH-2026-08-11.md) | The landscape and framework research this design stands on |

Agents and contributors read the documents before working. The active slice
brief is the build boundary; the Decision Log is the only path for changing
a recorded decision.

## Relationship to CUETools 2026

The engine and the shared app core are owned by
[LynxTWO/cuetools_2026](https://github.com/LynxTWO/cuetools_2026) and
consumed here as a pinned dependency. Contracts and assurance invariants
flow from that fork; this repository never patches them locally.

## License

GPL-2.0-or-later, matching the engine. See [LICENSE](LICENSE).
