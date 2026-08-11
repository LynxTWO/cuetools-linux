# CUETools Linux

A native Ubuntu-first desktop app bringing the CUETools 2026 experience to
Linux: rip with calibrated assurance, verify, repair, and convert, matching
the WPF build's look and function in a package a tenth the size.

**Status: designed, not yet built.** The full architecture and engineering
interview is complete; the first slice (Verify) is proposed. Start reading
at [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

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
