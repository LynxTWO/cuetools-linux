# CUETools Linux Engineering Document (EDD)

Version: 0.2 (Audited). Date: 2026-08-11. Authors: Daniel Boyd, with Claude
(Conductor). Status: Audited, awaiting owner review.
Companion documents: ARCHITECTURE.md, DECISION-LOG.md, TRIAGE.md, and the
slice briefs (SLICE-001 onward).

This document is the rules for placing pieces. The Architecture Document
holds the puzzle itself. Where a section depends on an architecture decision,
it references the ADD section number instead of restating it.

```text
INTERVIEW STATE
Last completed:   Phase 3 EDD complete; all 18 sections Confirmed 2026-08-11
Next:             Phase 5 slice selection (SLICE-001)
Open questions:   Q-001 to Q-003 (section 4.3), scheduled as spikes
Statuses pending: none
```

## 1. One-Page Overview

- **Build philosophy in one line:** evidence over claims, small is a
  feature, the fork is upstream.
- **The three goals that outrank the rest:** honest and observable; small
  and fast (D-025); accessible (D-027).
- **The verification standard:** tests, logs, diffs, and observed behavior.
  An agent saying it worked is not a test result.
- **Current build boundary:** the active slice brief, per ADD section 15.
  Nothing outside it gets built without a Decision Log entry.

## 2. Engineering Principles

1. Assurance honesty above all: nothing is presented as verified that the
   policy has not verified.
2. Evidence over claims: measured, verified, inferred, unknown are different
   words; receipts back build and release claims.
3. Boring and mature beats novel until novelty pays rent.
4. The fork is upstream: contracts and invariants flow from it, never
   fork-and-drift.
5. Small is a feature: the size budget is a requirement, not an aspiration.
6. One owner per fact: data, contracts, and strings each have exactly one
   home.
7. Every shortcut is labeled and logged, or it does not happen.

(D-026.)

## 3. System Goals

| Goal | Target | How measured |
| --- | --- | --- |
| Honest | No surface presents unverified output as verified; every core-loop failure leaves a trace | Log review per release; guardrail 8 check in review |
| Small | Download <= 30 MB per package; installed <= 60 MB | CI size gate on packaging jobs |
| Fast | Cold start to interactive <= 2 s on mid-range hardware; rip drive-bound; verify/convert saturate cores | Startup timer log line; job throughput logs |
| Accessible | Core flows keyboard-operable; controls exposed via AT-SPI2 | Manual keyboard pass plus Orca smoke per release |
| Maintainable (unprotected) | A newcomer ships a small change in under a day | Observed |

Protected three per D-027: honest, small and fast, accessible.

## 4. Requirements Ledger

### 4.1 Confirmed requirements

| ID | Requirement | Acceptance test |
| --- | --- | --- |
| R-001 | Verify an existing rip (cue + audio) against AccurateRip and CTDB with a truthful per-track verdict and a named report | Given a known-good fixture album, when verified online, then AR and CTDB results display and a dated report file is written |
| R-002 | Repair a damaged rip via CTDB parity into a sibling copy; source byte-for-byte untouched | Given a corrupted fixture with CTDB coverage, when repaired, then the output verifies clean and the source hash is unchanged, with repair evidence written |
| R-003 | Convert album images across cue styles and formats with cue fidelity (gaps, pregap, embedded cues preserved) | Round-trip conversion preserves decoded PCM hash and cue semantics |
| R-004 | Rip audio CDs through the assured pipeline: calibration gate, cache defeat, C2 evidence, Test & Copy, held states, per fork invariants | On a calibrated drive, a rip produces evidence semantically matching the WPF app; secure modes fail closed uncalibrated |
| R-005 | Offline degradation with deterministic backfill (verification lane) | A verify run offline shows "not verified" and writes a journal entry; on reconnect, backfill appends dated verification evidence without altering the original log |
| R-006 | Size and startup within D-025 numbers | CI size gate; startup timer on reference hardware |
| R-007 | The 2026 visual identity, dark and light, at runtime | Side-by-side review against the WPF app; theme toggle switches live |
| R-008 | Accessibility per section 3 | Manual keyboard pass and Orca smoke on the core loop |
| R-009 | Every web lookup the WPF app performs works on Linux | Metadata lookup for a fixture disc returns CTDB-proxied results; artwork lookup returns art |
| R-010 | .deb and AppImage install and run on clean Ubuntu LTS | Fresh container/VM: install, launch, run the core loop |

### 4.2 Assumed requirements

| ID | Assumption | How it gets verified |
| --- | --- | --- |
| A-001 | X11 via XWayland is acceptable at v1; native Wayland can wait behind a flag | Post-release user feedback; Avalonia promotion timeline |
| A-002 | NativeAOT works with Avalonia 12.1 + engine + shared core | Publish spike early in the build order; fallback is trimmed self-contained (D-030) |
| A-003 | The five native codecs (libFLAC, lame, wavpack, MACLib, hdcd) build as .so from the fork's pinned sources | Codec build spike |

### 4.3 Open questions

| ID | Question | Blocks what | Close by |
| --- | --- | --- | --- |
| Q-001 | Which external command encoders ship on Linux v1 (opus, vorbis, musepack have native builds; qaac and TAK stay Windows-only) | Convert feature completeness | Convert slice planning |
| Q-002 | CLOSED 2026-08-11 (spike S-2): on Linux the fingerprint is a SHA-256 hash of the machine name only | - | Closed |
| Q-003 | CLOSED 2026-08-11 (spike S-1): staging, build, and execution all pass under pwsh/SDK 10 | - | Closed |

**Ledger rules.** Every requirement has an observable acceptance test.
Assumptions get verified and promoted, or corrected, never silently adopted.
Open questions close before anything they block gets built.

## 5. Data Model

Field-level truth for the app-owned entities of ADD section 7. Engine-owned
records (jobs' audio evidence, local DB) keep the engine's formats; the app
does not redefine them.

```text
ENTITY: BackfillJournalEntry
Purpose: pending network-dependent work for one completed local job.
Owned by: Job orchestration (ADD section 4).
Fields: id (ULID, required); jobId (required); discId (CDTOC id, required);
        lane (verification | enrichment, required); createdUtc (required);
        state (pending | resolved | unresolvable, required);
        attempts (int, required, default 0); lastAttemptUtc (optional);
        resolutionEvidencePath (optional); reason (required when
        unresolvable); formatVersion (required).
Relations: belongs to one job record; references evidence artifacts by path
        plus hash.
Deletion rule: resolved entries archive with the job. Unresolvable entries
        persist until the user discards; discarding writes a log line and
        never deletes evidence.
```

```text
ENTITY: DriveProfile
Purpose: capability and calibration record per physical drive.
Owned by: Drive access (ADD section 4).
Fields: deviceId (vendor + model + serial, required); capabilities (C2
        support, speeds, proven cache behavior; required once probed);
        calibrationVersion (required); readOffset (required once
        calibrated); calibratedUtc (optional); provenFlushSize (optional);
        formatVersion (required).
Relations: jobs reference the profile snapshot they ran under.
Deletion rule: removable; recalibration recreates. Jobs keep their snapshot
        copies.
```

**Model rules.**

- UTC everywhere. ULIDs for app-owned records.
- Every app-owned format carries formatVersion; changes only through
  versioned migrations with forward-compatible reads (ADD section 12), and
  format changes sit on the agent stop list (D-028).
- The connection that must never break: job -> disc identity -> evidence.
  Evidence without a disc identity is not evidence.

## 6. Permissions and Access Model

Single user, local data. No roles, no admin surface. Enforcement point:
filesystem permissions of the user's own account. (Confirmed by tier rule.)

## 7. Security Requirements

Tier 2 baseline (no risk flags):

- [ ] Secrets live in the desktop Secret Service (libsecret) via Platform
      services; never in code, config, or version control.
- [ ] No passwords stored; no accounts exist (ADD 8.5).
- [ ] Encrypted transport everywhere the server offers it: AccurateRip and
      artwork over HTTPS. Documented exception: CTDB is plain HTTP because
      the server lacks TLS; tracked upstream as fork decision D2. CTDB
      payloads are non-personal disc data; a failed or tampered response
      degrades confidence and can never corrupt local files (ADD guardrail 6).
- [ ] Input validated at every boundary: cue parsing and network responses
      (engine hardened per fork R1/R15 work).
- [ ] Native code trust: codec .so files resolve only through a hash-pinned
      manifest, carrying the fork's PluginTrustManifest posture adapted from
      PE/LoadLibrary to ELF/dlopen. No PATH or bare-name loading.
- [ ] Dependency vulnerability scanning: Dependabot plus a CI check on a
      schedule.
- [ ] Audit trail: the evidence logs are the audit trail for every job.
- [ ] Backups: user audio is the user's own; the journal and drive profiles
      live under XDG paths documented for backup inclusion.
- [ ] Rate limiting: not applicable, no public endpoints.

## 8. Privacy and Data Handling

- **Personal data inventory:** no accounts, no analytics, no telemetry.
  Optional proxy credentials live in the Secret Service. CTDB submissions
  include disc TOC and rip evidence plus a machine fingerprint from the
  DeviceId package; what that package emits on Linux is Q-002, verified
  before the submission path ships, and documented in the privacy text.
- **Retention:** everything is local and user-controlled. The app deletes
  nothing without an explicit user action.
- **Sharing:** CTDB (submissions as above); AccurateRip, metadata, and
  artwork providers receive disc identifiers in lookups. Nothing else.
- **User-facing legal:** adapt the fork's PRIVACY.md for Linux; linked from
  the app and README before the first public release.

## 9. Coding Standards

- **Language and typing:** C# on .NET 10 SDK; nullable enabled; .NET
  analyzers on; warnings as errors in CI.
- **Naming:** standard .NET conventions; Avalonia views end in View
  (.axaml), view models in ViewModel.
- **Functions:** single purpose; validate inputs at module boundaries.
- **Errors:** handled or deliberately propagated, never swallowed;
  user-facing errors written for humans; failure context scrubbed per fork
  rules (never sector payload bytes).
- **Logging:** structured via Microsoft.Extensions.Logging; levels used
  honestly; never log secrets, tokens, or payload bytes.
- **Comments:** why, not what; stale comments are bugs.
- **Accessibility:** every interactive element labeled; AT-SPI2 exposure is
  part of control acceptance.
- **User-facing text:** one strings home from day one (ADD section 3).
- **Formatting:** dotnet format enforced in CI; markdownlint for docs; the
  fork's ASCII writing rules (no em dashes, no typographic Unicode) apply to
  all human-facing text.
- **AI-generated code:** held to every rule above and reviewed with the same
  care as human code.

## 10. Repository Organization

- **Top-level layout:**
  - `src/` - CUETools.Linux.App (shell), CUETools.Linux.Drive,
    CUETools.Linux.Codecs, CUETools.Linux.Platform, and tests beside them
    in `tests/`.
  - `extern/cuetools_2026/` - the fork submodule (engine + shared app core),
    pinned (D-014).
  - `docs/` - these documents; agents read them before working.
  - `eng/` - build, packaging (.deb, AppImage), and CI scripts.
- **Feature organization:** by module, matching the ADD map.
- **Shared code location:** the fork submodule is the shared home; an
  app-local Common project appears only when duplication earns it.
- **Dependency direction:** see ADD section 4.
- **Setup:** README steps take a clean Ubuntu machine to a running local
  build; re-tested whenever they change.

## 11. Testing and Verification

**The standard.** A claim of working requires at least one of: a passing
automated test, a log line showing the behavior, a diff plus observed
output, or a reproducible manual script with its recorded result.

**T2 test types:**

- Unit tests on logic (journal state machine, codec resolution, naming).
- Integration tests on module seams: drive shim against a fake SG_IO device,
  journal replay against a mock adapter, codec manifest resolution.
- Avalonia.Headless tests for page logic and bindings.
- A smoke test running the core loop end to end on a committed fixture album
  in CI.
- Theme and visual checks: manual side-by-side pass per release (R-007).

**Verification ledger** (grows with the build; seeded):

| Requirement | Check | Evidence lives |
| --- | --- | --- |
| R-001 | CI smoke: verify fixture album | CI run log |
| R-005 | Journal replay integration test | CI run log |
| R-006 | CI size gate | Packaging job output |
| R-010 | Clean-container install test | Packaging job output |

**Test data rule.** Fixtures are small, generated (tone tracks), and
committed; no copyrighted audio ever enters the repository.

## 12. Tool and Agent Discipline

Modes per the kit table: Discovery, Planning, Implementation, Verification;
one mode at a time, narrowest permissions.

**Standing rules.**

- Read before writing. Inspect before modifying.
- One purpose per tool call.
- The current build boundary is the active slice brief per ADD section 15.
  Work outside it requires a Decision Log entry first.
- When evidence contradicts the plan, stop and surface it.

**Stop list (D-028): always require explicit owner approval.**

1. Deletions outside the build boundary, anything touching secrets, any
   deploy (baseline).
2. New dependencies, NuGet or native.
3. Submodule pin bumps and PRs against the fork.
4. Releases, tags, and repository settings.
5. Schema-class changes to the backfill journal or evidence formats.

## 13. Observability (T2)

- Logs answer what happened; there are no analytics.
- **Always logged:** job start, end, and outcome; external call outcomes;
  errors with scrubbed context; one "core loop succeeded" line per job.
- **Never logged:** secrets, tokens, sector payload bytes, personal data.
- Structured logs under the XDG state directory with rotation; log review is
  the per-release honesty check (section 3).

## 14. Operations and Deployment

- Environments and release path: ADD section 12, by reference.
- **CI gates before merge:** build, tests, dotnet format, markdownlint,
  and the size gate on packaging jobs.
- **Migrations:** formatVersion-driven for app-owned records (section 5).
- **Configuration:** XDG config file; code never contains secrets.
- Release checklist: section 17.

## 15. Cost Discipline

- **Monthly ceiling at launch:** $0 infrastructure (public GitHub, free CI
  tiers).
- **Managed first:** rent before build; the fork before the ecosystem before
  new code.
- **Cost triggers:** none monetary; CI minutes reviewed if packaging jobs
  grow past the free tier.
- **Engineering time:** build only what differentiates (assurance, identity,
  Linux integration); everything else is reused.

## 16. Risk Register and Unknowns

| ID | Risk or unknown | Impact if wrong | Verification or mitigation | Status |
| --- | --- | --- | --- | --- |
| U-001 | Experience-level card entry (Assumed) | Low | Correct if it matters | Watching |
| U-002 | Budget posture near zero (Assumed) | Signing/store fees blocked | Confirm when fees appear | Watching |
| U-003 | Fork's Linux drive access approach | - | CLOSED: SG_IO shim proven | Closed |
| U-004 | Display-server posture | - | CLOSED: X11 default, Wayland opt-in | Closed |
| U-005 / Q-003 | Vendor staging under pwsh on Linux | Engine builds blocked | CLOSED by spike S-1: staging, build, and run all pass (SPIKES-2026-08-11.md) | Closed |
| U-006 / Q-002 | DeviceId behavior on Linux | CTDB submissions; privacy text | CLOSED by spike S-2: hashed machine name only, stable (SPIKES-2026-08-11.md) | Closed |
| A-002 | NativeAOT compatibility | Publish mode falls back | S-3 proved the UI toolchain (32.8 MB installed, ~440 ms to window). Residual A-002b: engine's Newtonsoft.Json under AOT untested; trimmed fallback stands | Watching |
| A-003 | Native codecs build as .so | Codec runtime blocked | Codec build spike | Open |
| Q-001 | Linux v1 external encoder set | Convert completeness | Close at convert slice planning | Open |
| O-001 | CTDB status line shows an XML parse quirk on the unknown-disc path ("error in XML document (0,0)"), observed 2026-08-11 during the first live verify on Linux | Cosmetic verdict text; verdict itself honest | Compare against WPF behavior for the same unknown TOC; fix where the truth lives | Watching |

## 17. Definition of Done

**Per change:** builds clean, lints clean, formatted; tests exist and pass
with evidence linked per section 11; errors on the new path handled and
logged; no secrets, debug leftovers, or dead code; documents and Decision
Log updated if any decision changed; deliberate self-review against this
checklist.

**Per release:** migrations applied and reversible; rollback path confirmed
per ADD section 12; checks in place per section 13; version tagged with a
changelog entry; size gate passed (D-025); **final approval: Daniel Boyd**
(D-029).

## 18. Change Control

- Documents start at v0.1 Draft; a full audit pass bumps and marks Audited.
- Status lifecycle per the kit: Proposed becomes Confirmed or is replaced;
  Assumed is verified or corrected; Open closes before dependent work;
  Deferred reopens on its trigger.
- No silent edits: decision changes travel through DECISION-LOG.md.

---

*Sections filled: 18 of 18, all Confirmed. Unknowns carried: see section 16.
See DECISION-LOG.md for reasoning.*
