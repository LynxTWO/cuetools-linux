# CUETools Linux Slice Brief: SLICE-006 Settings Persistence

Version: 0.1. Date: 2026-08-13. Status: Approved for build (owner selected
2026-08-13, D-043).
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md,
SLICE-005-codec-runtime.md.

## 1. What the slice adds, and why it is next

- **Capability:** settings survive restarts. The shared SettingsStore
  (extracted from the WPF head, fork PR #20) loads the profile at
  startup and saves it on every graceful exit: the engine's complete
  CUEConfig via its own battle-tested profile Save/Load, plus every
  AppSettings field (output folders, naming scheme and switches, format
  and codec selections, correction quality, and the rest).
- **Why now:** owner selected it (D-043) - the app forgot everything
  between launches, and the gap grew with every page shipped.

## 2. Design

- Same profile, same format, same location family as the WPF head: the
  classic key=value `settings.txt` under the ApplicationData
  `CUETools2026` directory (`~/.config/CUETools2026` on Linux). A
  profile written by one head loads in the other where the fields
  overlap.
- **Secrets are never persisted on this head.** The store's protector
  seam (`ISecretProtector`, public in the app core) gets a
  `DecliningSecretProtector`: Protect returns empty (the store skips the
  write), and Unprotect of a foreign protected value declines, which the
  store already treats as no-credential-until-set-again. No plaintext
  fallback exists.
- **POSIX signals are part of exit.** A Linux session manager stops apps
  with SIGTERM; the app now routes SIGTERM/SIGINT through the graceful
  Avalonia lifetime shutdown, and the save runs from the lifetime's Exit
  event (which fires on every graceful path - ShutdownRequested does not
  cover a forced shutdown; measured, not assumed).

## 3. Acceptance criteria

| ID | Criterion | Verified by |
| --- | --- | --- |
| S6-001 | Settings round-trip across store instances on Linux (config + app fields) | SettingsPersistenceTests |
| S6-002 | Secrets never reach the profile; a foreign protected credential degrades to no-credential | SettingsPersistenceTests |
| S6-003 | A real app run saves on SIGTERM and the next run loads the profile | Live evidence run |
| S6-004 | Owner walkthrough | Owner sign-off (met 2026-08-13, morning walkthrough) |

## 4. Verification evidence

- [x] SettingsPersistenceTests: round trip, secret exclusion, foreign
      credential degradation (suite 38/38).
- [x] Live run (2026-08-13): first run SIGTERM'd -> graceful shutdown ->
      `settings.txt` written with 398 keys; second run logs "settings
      loaded" and saves again on exit.
- [ ] EDD section 17 per-change checklist per change.

## 5. What this unlocks

- SLICE-007 external command encoders (persisted approvals now stick).
- A future Settings page edits a profile that actually persists.

---

*Approved for build by: Daniel Boyd, 2026-08-13 (D-043 selection).*
