# CUETools Linux - Triage Card

Kit: Scaffold Kit v0.3 (Field-Tested). Conductor protocol.
Date: 2026-08-11.

```text
INTERVIEW STATE
Last completed:   Phase 1 Triage
Next:             Phase 2 ADD Section 2 (System Context), then Section 3 (Product Shape and Platforms)
Open questions:   none carried
Statuses pending: experience level (Assumed), budget posture (Assumed)
```

```text
TRIAGE CARD
Project name:        CUETools Linux
One-line summary:    Native Linux desktop app bringing the CUETools 2026 WPF experience
                     (rip with calibrated assurance, verify, convert, repair) to Ubuntu.
Project type:        app
Starting point:      existing code (engine: LynxTWO/cuetools_2026 fork; GUI: greenfield).
                     Mapping pass exists: fork docs/architecture/system-map.md and
                     repo-slices.md, plus the 2026-08-11 port inventory (RESEARCH doc
                     and the exploration report in the interview session).
Run mode:            full interview
Scale tier:          T2 (real users, expected to live, solo maintainer)
Team shape:          solo (with AI agents)
Experience level:    built some things (Assumed: experienced builder, chose guided
                     depth; verify with owner if it starts to matter)
Interview depth:     guided
Risk flags:          none (no payments, no personal data beyond local files and
                     optional proxy credentials)
Platform targets:    Ubuntu x64 first; other distros via packaging choices later
Timeline posture:    weeks (staged: first working slice in days-to-weeks, parity
                     over months)
Budget posture:      near zero (Assumed)
```

## Phase 0 intake restatement (Confirmed 2026-08-11)

1. CUETools Linux is a native Ubuntu-first desktop app that brings the fork's
   modern WPF experience to Linux: rip with calibrated assurance, verify,
   convert, repair.
2. It is for audio archivists and CD preservationists on Linux, who today have
   no modern CUETools GUI.
3. The central thing it must do well: match the WPF build in looks and function
   while staying small on disk.
4. It builds on the fork's portable core libraries, not an engine rewrite.
5. Secondary goal: learn how to take a Windows-first repo cross-platform, with
   lessons feeding back into the scaffold kit and the anti-dark-code skill.

## Unknowns list

- U-001: exact experience-level entry on the card (Assumed, low stakes).
- U-002: budget posture (Assumed near zero; affects signing certificates and
  store distribution fees if those ever apply).
- U-003: CLOSED 2026-08-11. The UnknownException fork P/Invokes libc SG_IO
  directly on /dev/sr* through a LinDev shim under Bwg.Scsi; no libcdio, no
  cdparanoia. See RESEARCH-2026-08-11-unknowns.md.
- U-004: CLOSED 2026-08-11. Avalonia desktop Linux is X11 (XWayland on
  Wayland sessions); 12.1 adds an experimental opt-in native Wayland backend.
  See RESEARCH-2026-08-11-unknowns.md.
- U-005: CLOSED 2026-08-11 by spike S-1. Vendor staging, engine build, and
  engine execution all pass under pwsh 7.6 / SDK 10 on Ubuntu 24.04. See
  SPIKES-2026-08-11.md.
- U-006: CLOSED 2026-08-11 by spike S-2. On the netstandard/net10 path the
  CTDB fingerprint is a stable SHA-256 hash of the machine name only. See
  SPIKES-2026-08-11.md.
