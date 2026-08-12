# CUETools Linux Architecture Document (ADD)

Version: 0.2 (Audited). Date: 2026-08-11. Authors: Daniel Boyd, with Claude (Conductor).
Status: Audited, awaiting owner review.
Companion documents: ENGINEERING.md, DECISION-LOG.md, TRIAGE.md,
RESEARCH-2026-08-11.md, and the slice briefs (SLICE-001 onward).

This document is the puzzle: what the system is, what its major pieces are, how
they connect, and where future pieces will attach. The Engineering Document
holds the rules for placing pieces. Where the two conflict, this document's
guardrails control.

```text
INTERVIEW STATE
Last completed:   Phase 6 audit passed; SLICE-001 Approved for build
                  (Daniel Boyd, 2026-08-11)
Next:             SLICE-001 milestone M1: spikes S-1 (pwsh vendor staging),
                  S-2 (DeviceId on Linux), S-3 (NativeAOT publish)
Open questions:   Q-001 (encoder set, closes at convert slice planning)
Statuses pending: none
```

## 1. One-Page Overview

- **What it is:** CUETools Linux is a native Ubuntu-first desktop app
  bringing the CUETools 2026 experience to Linux: rip with calibrated
  assurance, verify, repair, and convert, matching the WPF build's look and
  function in a package a tenth the size.
- **Who it is for:** audio archivists and CD preservationists on Linux,
  publicly released from day one. No accounts, no server of its own.
- **The core loop:** pick an album (disc or files), run a job, get an honest
  verdict backed by immutable evidence.
- **Major pieces:** Desktop shell, Shared app core (fork), Job orchestration
  and evidence, Engine (fork), Drive access (Linux), Codec runtime (Linux),
  Platform services (Linux).
- **Current slice:** set at Phase 5 slice selection (see section 15).
- **What this is not:** a player, a library manager, a DVD/Blu-ray tool, or
  a Windows replacement. The WPF app remains the Windows product.

## 2. System Context

- **Actors:**
  - Primary user: a Linux audio archivist or CD preservationist who rips,
    verifies, repairs, and converts lossless CD rips. Public release from day
    one; no account, no server, no admin role.
  - Maintainer: Daniel Boyd (solo), with AI agents as contributors that read
    the project documents before acting.
- **External systems** (from the fork's system map, carried over):
  - AccurateRip (`www.accuraterip.com`, HTTPS): read-only rip-accuracy lookups
    and drive offset table.
  - CUETools Database (CTDB, db.cuetools.net): verify lookups, repair parity
    data, and rip submissions. Also the proxy through which MusicBrainz
    metadata arrives.
  - gnudb/Freedb: metadata fallback, read-only.
  - Cover art services (Cover Art Archive, TheAudioDB): artwork lookup,
    read-only.
  - Local OS: optical drives (Linux device layer), filesystem, desktop
    environment (file dialogs, notifications).
- **Boundary statement:** CUETools Linux owns ripping audio CDs with assurance
  evidence, verifying and repairing existing rips against AccurateRip and CTDB,
  converting between lossless (and selected lossy) formats with cue-sheet
  fidelity, and the metadata/artwork lookups those jobs need. Everything
  web-based that Windows CUETools 2026 looks up, CUETools Linux can look up.
  Explicitly outside: music playback, music library management and tag editing
  beyond what a rip or conversion writes, DVD/Blu-ray tooling, and the
  Windows-only surfaces of the fork (CTDB EAC plugin, WMA codec, DirectSound,
  classic WinForms GUIs). Those stay in the Windows fork. EAC has no Linux
  variant, so the EAC plugin has no Linux job.

DECISION: D-005 Audience posture (see DECISION-LOG.md)
DECISION: D-006 Scope boundary (see DECISION-LOG.md)

## 3. Product Shape and Platforms

- **Shape:** desktop app. One local application; no server component of our
  own.
- **Platform targets:** Ubuntu LTS x64 first (22.04/24.04-class). Other
  distros reached through the AppImage channel. Display-server posture (X11
  vs native Wayland) is unknown U-004, resolved with the client decision in
  section 8.1.
- **Offline behavior:** degraded, with deterministic backfill. Rip and convert
  work fully offline. Verify, repair, metadata, and artwork lookups need
  network and report "not verified / unavailable" without blocking or mutating
  local work. Offline-completed jobs record immutable evidence plus a backfill
  journal entry; when network returns, verification backfill replays
  automatically and appends dated named evidence, while enrichment backfill
  (tags, artwork, renaming) queues as proposals the user explicitly applies.
  A backfilled verdict is deterministic given the evidence and that day's
  database answer; reports record when they ran.
- **Distribution:** .deb package (primary, Ubuntu) and AppImage (portable,
  cross-distro). Flatpak deferred (revisit: demand for store presence, and
  sandbox permissions vs raw optical-drive access). Plain tarball not a
  supported channel at v1.
- **Languages at launch:** English. All user-facing text kept in one place so
  translation later is a move, not a rewrite. (Stated default, accepted.)
- **Code license:** GPL-2.0-or-later, matching the engine (fork License.txt
  lines 8-9).

DECISION: D-007 Distribution channels (see DECISION-LOG.md)
DECISION: D-008 Code license (see DECISION-LOG.md)
DECISION: D-009 Repository shape (see DECISION-LOG.md)
DECISION: D-010 Offline behavior and backfill (see DECISION-LOG.md)

## 4. Module Map

| Module | Responsibility | Owns what data | Talks to |
| --- | --- | --- | --- |
| Desktop shell | Windows, pages, navigation, theming, visualizations (disc, VU, speed graph) | UI state, view preferences | Shared app core, Platform services |
| Shared app core (fork dependency) | Platform-neutral page logic: ViewModels and portable app services extracted from CUETools.Wpf | None locally (pure library) | Engine, Job orchestration seams |
| Job orchestration and evidence | Runs rip / verify / repair / convert as transactions; queue; reports; backfill journal | Job records, evidence artifacts, backfill journal | Engine, Drive access, Codec runtime |
| Engine (fork dependency, pinned) | Cue/audio processing, AccurateRip + CTDB + metadata clients, managed codecs, parity repair | None locally (pure library) | External services |
| Drive access (Linux) | ICDRipper implementation over SG_IO, drive enumeration, calibration, tray control, drive leases | Drive capability and calibration records | Kernel (/dev/sr*), Job orchestration |
| Codec runtime (Linux) | Native .so builds of codec libraries, resolution and trust manifest, user-imported encoders | Codec manifest, imported-encoder registry | Engine, filesystem |
| Platform services (Linux) | Secrets storage, XDG paths, file dialogs, keep-awake, notifications | Settings store, secrets | Desktop environment |

**Module rules.**

- Every module has exactly one primary responsibility.
- Every piece of data has exactly one owning module. Others read through
  interfaces.
- Dependency direction: shell depends on shared app core and platform
  services; orchestration depends on engine, drive access, and codec runtime;
  nothing depends downward on the shell; the engine never calls upward.
- The shared app core and the engine are owned by the fork repository and
  consumed as pinned dependencies. CUETools Linux never patches them locally;
  changes flow through the fork's PR process.

DECISION: D-011 Backfill placement (see DECISION-LOG.md)
DECISION: D-012 UI logic sharing strategy (see DECISION-LOG.md)
DECISION: D-013 Module map (see DECISION-LOG.md)

## 5. Interfaces and Contracts

- **Interface style:** typed function boundaries inside each codebase; across
  repositories, versioned library dependencies (engine and shared app core,
  both fork-owned, both pinned); every external service sits behind an engine
  adapter.
- **Public interfaces:**
  - `ICDRipper` (CUETools.Ripper): the drive seam. The Linux drive-access
    module implements it over SG_IO.
  - Codec source/destination contracts (CUETools.Codecs): what the Linux
    codec runtime satisfies with native .so builds.
  - Shared app core surface: ViewModels and portable service interfaces
    extracted from CUETools.Wpf (D-012).
  - Processor job API (CUETools.Processor): rip, verify, repair, convert
    transactions.
  - No public CLI or API of CUETools Linux's own at v1.
- **Contract rule:** interface shapes are defined in the fork, one source of
  truth. CUETools Linux implements fork-defined contracts and never redefines
  them.
- **Versioning posture:** the Linux app pins an exact fork commit (git
  submodule, built from source as project references). Updates are
  deliberate, reviewed bumps, never floating. Building the fork's projects on
  Linux requires the vendor-staging step under PowerShell Core: unknown
  U-005, spike scheduled before dependent work.

DECISION: D-014 Engine consumption mechanism (see DECISION-LOG.md)
DECISION: D-015 Interface style and versioning posture (see DECISION-LOG.md)

## 6. Core Data Flow

The core loop, narrated for the verify journey:

1. User opens the app and picks an album folder or cue sheet.
2. The engine parses the cue and decodes the audio.
3. Checksums are computed (AccurateRip, CTDB, CRC evidence).
4. AccurateRip and CTDB are queried. Offline, the job writes a backfill
   journal entry instead.
5. The verdict and per-track evidence render in the shell.
6. If damage is found and CTDB parity covers it, the user triggers repair:
   a repaired sibling copy is written with named evidence; the source is
   never touched.

Rip and convert follow the same transaction pattern (job in, immutable named
evidence out); their step lists derive from the WPF app's transactions and
carry the same held-state and publication rules.

- **Trigger points:** user actions; automatic verification-backfill replay on
  network return.
- **Slow paths:** decode and checksum show per-track progress; rips are
  long-running and cancellable; network lookups have timeouts and never block
  local work.
- **Failure path:** unreadable cue: actionable parse message. Network
  failure: "not verified" plus a journal entry. Failed repair: source
  untouched, stated plainly. Drive errors during rip: the fork's evidence and
  retry policy, carried over intact.

## 7. Data Domain Overview

- **Entities:**
  - Album job: one rip / verify / repair / convert transaction.
  - Disc identity: the table of contents keying every AccurateRip and CTDB
    lookup.
  - Track evidence: per-track checksums and read results.
  - Evidence artifact: named immutable logs, reports, and proofs a job
    publishes.
  - Backfill journal entry: pending verification or enrichment for one job.
  - Drive profile: capability and calibration records per physical drive.
  - Codec registration: available implementations, manifests, imported
    encoders.
  - Settings: application configuration and preferences.
- **Key relationships:** a job references exactly one disc identity and
  produces many track-evidence rows and evidence artifacts. A backfill entry
  belongs to one job. Calibration records version per drive. Codec
  registrations are global.
- **Volume expectations:** personal archives run hundreds to low thousands of
  albums. Evidence grows linearly with jobs. The journal stays small (offline
  jobs only).

## 8. Technology Selection

Research inputs: docs/RESEARCH-2026-08-11.md.

### 8.1 Client

Avalonia. WPF-shaped XAML and MVVM, pixel-identical Skia rendering on Ubuntu,
real applications at 25 to 40 MB uncompressed and 15 to 30 MB packaged. The
one working Linux port in this code family (UnknownException's CUERipper)
chose it independently. Version, .NET target, publish mode, and theming are
sub-decisions recorded at 8.1a after U-003/U-004 evidence lands.

DECISION: D-016 Client technology (see DECISION-LOG.md)

### 8.2 Language

C#. The engine and shared app core already are.

DECISION: D-017 (see DECISION-LOG.md)

### 8.3 Backend

None, client only. Every service is a third-party public database.

DECISION: D-018 (see DECISION-LOG.md)

### 8.4 Database

None of our own. Local structured files under XDG paths: engine local DB,
backfill journal, settings. Formats specified in ENGINEERING.md section 5.

DECISION: D-019 (see DECISION-LOG.md)

### 8.5 Authentication

None, local only. No accounts anywhere in the product.

DECISION: D-020 (see DECISION-LOG.md)

### 8.6 AI layer

None in-product.

DECISION: D-021 (see DECISION-LOG.md)

### 8.7 Notifications and messaging

In-app progress always; optional desktop notification when a long job
finishes.

DECISION: D-022 (see DECISION-LOG.md)

### 8.1a Versions, publish mode, display posture (sub-decision of 8.1)

Avalonia 12.1.x on .NET 10. Compiled bindings (default in 12) throughout.
Publish: NativeAOT primary, trimmed self-contained fallback if a dependency
breaks AOT. Display: X11 default (XWayland on Wayland sessions); native
Wayland behind an opt-in flag via the Avalonia.Wayland package until Avalonia
promotes it. The engine stays netstandard2.0; the shared app core targets
net8.0 so both the WPF app (net8.0-windows) and this app (net10.0) consume
it. Evidence: RESEARCH-2026-08-11-unknowns.md (U-004).

DECISION: D-030 Client versions and publish mode (see DECISION-LOG.md)

### 8.1b Visual identity (sub-decision of 8.1)

Port the 2026 theme. The fork's designed identity (lamp-glow accents, teal
and amber palette, serif and mono font stacks, BendyButton, VU meter, runtime
dark/light) is recreated in Avalonia styles. The fork's visualization skills
(lit-panel-controls, codec-visualization, disc-read-visualization) document
the patterns as portable.

DECISION: D-023 Visual identity (see DECISION-LOG.md)

### 8.8 Hosting and builds

Public GitHub repository LynxTWO/cuetools-linux, created at interview end
with these documents as the first commit, master protected by the same
PR-required ruleset as the fork. GitHub Actions on Ubuntu runners: build,
tests, and packaging artifacts (.deb and AppImage) on every PR; tagged
releases publish artifacts with SHA-256 checksums. GPG release signing is
Deferred as its own future decision.

DECISION: D-024 Hosting and build pipeline (see DECISION-LOG.md)

## 9. Integration Map

| External service | Purpose | Direction | Failure behavior |
| --- | --- | --- | --- |
| AccurateRip | Rip-accuracy lookups, drive offset table | read | "Not verified" plus a backfill journal entry; local work continues |
| CTDB | Verify lookups, repair parity, rip submissions, MusicBrainz metadata proxy | both | Verify degrades; repair unavailable; failed submission is non-fatal. Transport is still plain HTTP, tracked upstream as fork decision D2 |
| gnudb / Freedb | Metadata fallback | read | Fallback skipped |
| Cover Art Archive, TheAudioDB | Artwork lookup | read | Job continues without art |

**Adapter rule.** Satisfied inside the engine: each provider has one engine
adapter, and CUETools Linux never calls a provider directly. Swapping a
provider is an engine change through the fork's PR process.

## 10. Extension Points

| Future feature | Connects at | What exists now (planned) | Deliberately absent |
| --- | --- | --- | --- |
| Enrichment backfill (tags, artwork, renaming) | Journal's enrichment lane | Journal format with lane field; verification lane running | The apply-proposals UI |
| Flatpak channel | Packaging pipeline | .deb/AppImage pipeline | Portal and sandbox work |
| Headless CLI for scripting | Processor job API via shared core | The job API seam | Any CLI shell |
| Translations | Centralized strings location | One strings home from day one | Translation files |
| Engine via packages | Submodule pin (D-014) | Pinned source builds | Package publishing |
| GPU encode (FLACCL) | Codec runtime registry | Codec registration seam | OpenCL-on-Linux work |

**Rule.** An extension point is a named seam, not a built feature. Building
for the future means leaving a clean edge, not adding speculative code.

## 11. Scale and Performance Posture (T2+)

- **Load expectations:** single user, personal archives of hundreds to low
  thousands of albums; batch verifies across whole collections.
- **Performance targets (release-gating):** download <= 30 MB per package;
  installed <= 60 MB; cold start to interactive <= 2 seconds on mid-range
  hardware; rip throughput drive-bound with no regression against the WPF
  policy; verify and convert saturate available cores.
- **Scaling approach:** none needed beyond multi-core use; a desktop app for
  one user scales by doing less, not more.

DECISION: D-025 Performance targets (see DECISION-LOG.md)

## 12. Deployment Topology (T2+)

- **Environments:** local dev, CI, released artifacts. Pre-release tags for
  betas. A desktop app has no staging.
- **Release path:** PR -> CI green -> merge -> tag -> release workflow ->
  GitHub Release with .deb, AppImage, and SHA-256 checksums.
- **Rollback:** previous releases stay installable. Settings and journal
  formats are versioned with forward-compatible reads, so a downgrade never
  corrupts. A release that must change a format ships a migration note and a
  documented revert path.

## 13. Failure and Degraded Modes (T2+)

| Failure | User sees | System does | Recovery |
| --- | --- | --- | --- |
| AccurateRip or CTDB unreachable | "Not verified / unavailable" row | Journals verification for backfill; local work continues | Automatic on reconnect |
| Drive read errors mid-rip | The fork's evidence rows and honest log lines | The fork's retry and evidence policy verbatim: untrusted-media vs fatal classes preserved | Per policy; never silent |
| Native codec missing or hash-failed | Explanatory unavailable row before the job starts | Refuses the job up front (codec-readiness precondition); no silent fallback | User installs or re-imports codec |
| Secrets service unavailable | Plain message; proxy credentials unavailable | Fails closed, mirroring the DPAPI policy | User restores desktop secrets service |
| Backfill entry's files moved or changed | Entry marked unresolvable, with reason | Preserves the entry and evidence; proposes user actions | Manual |
| Crash mid-job | On restart: job in held state, nothing partial published | Transactional publication rules; held state survives restart | User resumes or discards |

## 14. Architecture Guardrails

1. No module reads another module's data files directly; access goes through
   the owning module's interface.
2. No external provider is called outside the engine's adapters.
3. The Linux repo never patches fork code in place; engine and shared-core
   changes go through the fork's PR process, and the submodule pin moves
   only by reviewed bump.
4. No platform-conditional code in the shared app core; platform behavior
   lives behind the platform-service interfaces.
5. Evidence artifacts are append-only; nothing rewrites a published log or
   report.
6. Network lookups never block or mutate local work.
7. No user-facing string outside the centralized strings home.
8. The fork's rip-path assurance invariants (calibration gating, cache
   defeat, held states) bind every Linux surface; no output is presented as
   assured that the policy has not assured.
9. No dependency lands that breaks the D-025 size budget without a logged
   superseding decision.

## 15. Current Build Boundary

- **Current slice:** SLICE-001 Verify: album verification with honest
  AR/CTDB verdict, named report, 2026 theme, journal seam, .deb and
  AppImage packaging (SLICE-001-verify.md).
- **Modules the slice touches:** Desktop shell, Shared app core (extraction
  in the fork), Job orchestration and evidence, Engine (consumed), Platform
  services.
- **Modules the slice stubs:** Codec runtime (honest unavailable rows,
  D-032). Drive access untouched.
- **Everything else:** designed above, deliberately unbuilt.

---

*Sections filled: 15 of 15. Unknowns carried: see EDD section 16 risk
register. See DECISION-LOG.md for reasoning.*
