# CUETools Linux - Decision Log

Kit: Scaffold Kit v0.3. Status vocabulary: Confirmed, Proposed, Assumed, Open,
Deferred, Superseded. Every significant choice gets an entry with a sequential
ID. The documents state what; this log preserves why and what else was
considered.

---

DECISION: D-001 Interview framework
STATUS: Confirmed
CHOICE: Run the Scaffold Kit v0.3 Conductor protocol for the CUETools Linux
design interview.
BECAUSE: T2-scale cross-platform port. The documents live on in the new
project. The kit gets another field test, and lessons flow back to it.
OPTIONS CONSIDERED: Lighter brainstorming flow (faster, less durable
documentation); hybrid (brainstorm now, retrofit kit docs later).
REVISIT WHEN: The interview overhead visibly outweighs its value for a
follow-on slice.

DECISION: D-002 Run mode and depth
STATUS: Confirmed
CHOICE: Full interview, guided depth.
BECAUSE: Owner wants each concept explained in plain language and full control
of each section. AI recommendation was fast-run; owner overrode.
OPTIONS CONSIDERED: Fast-run (defaults filled, veto in batches); standard or
expert depth.
AI RECOMMENDATION DIFFERED: fast-run was recommended; owner chose full
interview.
REVISIT WHEN: Interview pace becomes a problem across sessions.

DECISION: D-003 Product and repo name
STATUS: Confirmed
CHOICE: Product "CUETools Linux", repo name cuetools-linux.
BECAUSE: Shorter than "CUETools 2026 for Linux", keeps the CUETools lineage
readable, less tied to year branding.
OPTIONS CONSIDERED: "CUETools 2026 for Linux" (repo cuetools_2026_linux,
stronger fork lineage); a new name entirely (distinct identity, less
discoverable).
REVISIT WHEN: Upstream or trademark concerns surface, or the product diverges
enough from CUETools to deserve its own identity.

DECISION: D-004 Timeline posture
STATUS: Confirmed
CHOICE: Weeks, staged: first working slice in days-to-weeks, parity over
months.
BECAUSE: Matches the slice-based build model and the owner's pace.
OPTIONS CONSIDERED: Weekend (prototype pressure); months (depth-first with
nothing visible for a while).
REVISIT WHEN: The first slice's actual duration is known.

DECISION: D-005 Audience posture
STATUS: Confirmed
CHOICE: Public from the start: GitHub releases anyone can install, designed
for public users from day one.
BECAUSE: The research confirmed a real gap (no native Linux CUETools GUI;
upstream demand open since 2020). The fork's governance already produces
release-quality evidence. Public users keep parity honest.
OPTIONS CONSIDERED: Owner-first with public release at parity (slower
feedback); personal tool only (wastes the confirmed gap).
REVISIT WHEN: Public support load exceeds what a solo maintainer can carry.

DECISION: D-006 Scope boundary
STATUS: Confirmed
CHOICE: Out of scope: music playback, library management and tag editing
beyond rip/convert writes, DVD/Blu-ray tooling, and the fork's Windows-only
surfaces (CTDB EAC plugin, WMA, DirectSound, classic WinForms GUIs). In scope
without exception: every web-based lookup Windows CUETools 2026 performs
(AccurateRip, CTDB lookup and repair, MusicBrainz via CTDB proxy, gnudb,
cover art).
BECAUSE: Owner confirmed all four exclusions, noting EAC has no Linux variant,
and explicitly required full web-lookup parity. The exclusion is the EAC
plugin, never the CTDB service.
OPTIONS CONSIDERED: Including playback (different product; Linux has strong
players); including tag editing (beets/Picard/Kid3 own that space).
REVISIT WHEN: A future slice proposes any excluded area; requires a
superseding entry here.

DECISION: D-007 Distribution channels
STATUS: Confirmed
CHOICE: .deb package (primary) and AppImage (portable) at v1. Flatpak
Deferred. Tarball not a supported channel.
BECAUSE: .deb is native for the primary platform with a small download.
AppImage widens reach across distros for near-zero extra cost.
OPTIONS CONSIDERED: Flatpak (store visibility, but sandbox vs raw drive
access is awkward and the runtime dwarfs the app); tarball (zero effort,
weak as a channel).
REVISIT WHEN: Flatpak: users ask for store distribution, or drive-access
portals mature. Reopens as its own decision.

DECISION: D-008 Code license
STATUS: Confirmed
CHOICE: GPL-2.0-or-later for CUETools Linux, matching the engine.
BECAUSE: The engine is GPL-2.0-or-later (fork License.txt lines 8-9). One
license across engine and app; no compatibility analysis ever needed.
Irreversible-class: relicensing needs every contributor's consent, so this
got an explicit confirmation.
OPTIONS CONSIDERED: GPL-3.0-or-later (legal, adds patent protections, but the
combined work becomes effectively GPLv3 which some distributors avoid).
REVISIT WHEN: Practically never; a relicensing effort would be its own
project.

DECISION: D-009 Repository shape
STATUS: Confirmed
CHOICE: Separate repository LynxTWO/cuetools-linux (local:
DEV/apps/cuetools-linux), consuming the fork's engine projects as a pinned
dependency. Consumption mechanism (submodule vs packages) decided in ADD
section 8.
BECAUSE: Clean product identity, own issues/releases/CI. No risk to the
fork's frozen invariants and heavy release governance. Engine fixes flow
through the fork's own PR process.
OPTIONS CONSIDERED: Same repo, new top-level app (one PR spans engine and
app, but Windows CI and release ceremony land on every Linux change);
monorepo parent with submodules (ceremony without benefit at solo scale).
REVISIT WHEN: Engine changes needed by Linux become so frequent that
cross-repo friction dominates.

DECISION: D-010 Offline behavior and deterministic backfill
STATUS: Confirmed
CHOICE: Degraded-offline plus deterministic backfill. Local work (rip,
convert) runs fully offline. Network lookups degrade to "not verified /
unavailable" and never block or mutate local results. Offline jobs write a
backfill journal; on reconnect, verification backfill replays automatically
and appends dated named evidence, and enrichment backfill (tags, artwork,
naming) queues as user-approved proposals. Placement of the backfill UX in
v1 vs a later slice: see D-011.
BECAUSE: Owner extended the AI default (plain degraded-offline) with the
backfill concept. The engine's immutable-evidence discipline makes
verification a pure function of recorded evidence plus the database answer,
so replay is honest. Two lanes because verification is append-only while
enrichment mutates user files.
OPTIONS CONSIDERED: Plain degraded-offline (simpler, loses the follow-up
value); full online-required (hostile to a desktop tool).
REVISIT WHEN: Backfill journal format is designed (EDD data model).

DECISION: D-011 Backfill placement
STATUS: Confirmed
CHOICE: v1 ships the backfill journal seam plus the automatic verification
backfill lane. Enrichment backfill (tags, artwork, renaming proposals)
becomes a named extension point for a later slice.
BECAUSE: The journal format is painful to retrofit, so it exists from day
one. Verification backfill is append-only and safe to automate. Enrichment
mutates user files and deserves its own slice with an apply flow.
OPTIONS CONSIDERED: Both lanes in v1 (bigger, later ship); seam only
(offline verify results stay unresolved).
REVISIT WHEN: v1 ships; the enrichment slice picks up the extension point.

DECISION: D-012 UI logic sharing strategy
STATUS: Confirmed
CHOICE: Extract the shared library first. CUETools.Wpf is refactored in the
fork into a platform-neutral app core (ViewModels and portable services)
plus a WPF shell. CUETools Linux consumes the shared core as a pinned fork
dependency, like the engine.
BECAUSE: The page logic encodes assurance behavior (calibration gating, held
states, evidence publication) that must not drift between apps. Owner chose
to pay the refactor cost up front so both apps evolve from one source.
OPTIONS CONSIDERED: Copy-adapt now and extract later (learn the seam first,
extraction deferred); permanent copy-adapt (guaranteed drift); fresh
reimplementation (risks silently losing assurance semantics).
AI RECOMMENDATION DIFFERED: copy-adapt-then-extract was recommended; owner
chose extract-first.
REVISIT WHEN: The extraction proves too disruptive to the fork's WPF
surface; fallback is copy-adapt with a scheduled extraction.

DECISION: D-013 Module map
STATUS: Confirmed
CHOICE: Seven modules as tabled in ADD section 4: Desktop shell, Shared app
core (fork), Job orchestration and evidence, Engine (fork), Drive access
(Linux), Codec runtime (Linux), Platform services (Linux). Dependency
direction as stated there.
BECAUSE: Reality-derived from the fork's system map and the 2026-08-11 port
inventory; owner confirmed, including the shared-core row implied by D-012.
OPTIONS CONSIDERED: Six modules without the shared core (superseded by
D-012); folding drive access into orchestration (hides the only
platform-critical seam).
REVISIT WHEN: A slice cannot name which module owns its work.

DECISION: D-014 Engine consumption mechanism
STATUS: Confirmed
CHOICE: Git submodule pinned to an exact fork commit, built from source as
project references.
BECAUSE: Exact-commit pin with no package infrastructure, and fast iteration
while the shared-core extraction (D-012) is in flux. Requires proving the
fork's vendor staging under PowerShell Core on Linux: unknown U-005, spike
scheduled before dependent work.
OPTIONS CONSIDERED: NuGet packages from fork CI (clean binary boundary, but
publish infrastructure first and a publish cycle per engine tweak);
submodule now with a switch to packages at stabilization (two mechanisms
over time).
REVISIT WHEN: The shared core's surface stabilizes; packages become worth
their infrastructure.

DECISION: D-015 Interface style and versioning posture
STATUS: Confirmed
CHOICE: Typed function boundaries in-repo; pinned fork libraries across
repos; fork is the single source of truth for contracts; deliberate reviewed
version bumps.
BECAUSE: One contract owner prevents drift; pinning keeps the Linux app
reproducible; reviewed bumps keep engine changes visible.
OPTIONS CONSIDERED: Floating dependency on fork master (invisible breakage);
Linux-side contract copies (two sources of truth).
REVISIT WHEN: A contract change is needed that the fork will not take; that
event forces a product conversation, not a silent fork of the interface.

DECISION: D-016 Client technology
STATUS: Confirmed
CHOICE: Avalonia. Sub-decisions (version, .NET target, publish mode,
theming) recorded as D-023+ after U-003/U-004 evidence.
BECAUSE: WPF-shaped XAML and MVVM preserve the team's WPF investment.
Skia rendering is pixel-identical on Ubuntu, clearing the as-pretty-as-WPF
bar. Real applications land at 25 to 40 MB uncompressed, 15 to 30 MB
packaged, clearing the small-filesize bar against the 185 MiB Windows
folder. The UnknownException CUERipper port chose it independently.
OPTIONS CONSIDERED: GTK4/Gir.Core (smallest, but full rewrite, pre-1.0
bindings, Adwaita-locked look); Uno Platform (WinUI dialect, 2-3x size);
Photino (HTML rewrite against system WebKitGTK, fidelity risk).
REVISIT WHEN: Avalonia's Linux backends fail a v1 requirement (see U-004).

DECISION: D-017 Primary language
STATUS: Confirmed
CHOICE: C#.
BECAUSE: The engine and shared app core are C#; one language end to end.
OPTIONS CONSIDERED: None serious; any other language forfeits the engine.
REVISIT WHEN: Never expected.

DECISION: D-018 Backend approach
STATUS: Confirmed
CHOICE: None, client only.
BECAUSE: Every service the app talks to is an existing third-party public
database. No server-held secrets, no jobs of our own.
OPTIONS CONSIDERED: None serious.
REVISIT WHEN: A feature needs server-side state (none named).

DECISION: D-019 Database
STATUS: Confirmed
CHOICE: No server database. Local structured files under XDG paths: engine
local DB, backfill journal, settings.
BECAUSE: Relationship density is modest and local; the engine already has a
local DB format; files keep the app small and inspectable.
OPTIONS CONSIDERED: Embedded SQLite (revisit if journal queries outgrow
files).
REVISIT WHEN: EDD data-model work finds a query need files cannot serve.

DECISION: D-020 Authentication
STATUS: Confirmed
CHOICE: None, local only.
BECAUSE: No accounts anywhere in the product.
OPTIONS CONSIDERED: None apply.
REVISIT WHEN: Never expected.

DECISION: D-021 AI layer
STATUS: Confirmed
CHOICE: None in-product.
BECAUSE: No product feature calls for it. AI agents build the product; they
do not live inside it.
OPTIONS CONSIDERED: None.
REVISIT WHEN: A feature proposes it; new decision required.

DECISION: D-022 Notification channels
STATUS: Confirmed
CHOICE: In-app progress always; optional desktop notification on long-job
completion.
BECAUSE: Long rips and batch verifies finish while the user is elsewhere;
a desktop notification respects that without adding infrastructure.
OPTIONS CONSIDERED: In-app only (misses walk-away completion); email/push
(absurd for a local tool).
REVISIT WHEN: v1 user feedback.

DECISION: D-023 Visual identity
STATUS: Confirmed
CHOICE: Port the 2026 theme to Avalonia styles: lamp-glow accents, teal and
amber palette, serif and mono stacks, BendyButton, VU meter, runtime
dark/light toggle.
BECAUSE: "Every bit as pretty as the WPF build" means the same designed
identity, not a related one. The fork's visualization skills document the
patterns as portable.
OPTIONS CONSIDERED: FluentAvalonia base with 2026 accents (faster, related
look only); fresh Linux-native design (abandons the twin-app identity).
REVISIT WHEN: A specific control proves impractical in Avalonia; that
control gets a documented adaptation, not a theme change.

DECISION: D-024 Hosting and build pipeline
STATUS: Confirmed
CHOICE: Public repo LynxTWO/cuetools-linux created at interview end with the
documents as first commit; PR-required master ruleset; GitHub Actions Ubuntu
CI (build, tests, .deb and AppImage artifacts); tagged releases with SHA-256
checksums. GPG signing Deferred.
BECAUSE: Mirrors the fork's protections at Linux-appropriate weight.
OPTIONS CONSIDERED: Defer repo creation to build start (loses the document
home); release signing now (needs its own key-management decision).
REVISIT WHEN: GPG signing: first release with external users depending on
integrity; reopens as its own decision.

DECISION: D-025 Performance targets
STATUS: Confirmed
CHOICE: Release-gating numbers: download <= 30 MB per package; installed
<= 60 MB; cold start to interactive <= 2 seconds on mid-range hardware; rip
drive-bound with no regression vs the WPF policy; verify and convert
saturate cores.
BECAUSE: Comfortably achievable per RESEARCH-2026-08-11.md while staying 3x
smaller than the 185 MiB Windows folder. Numbers a release can pass or fail.
OPTIONS CONSIDERED: Aggressive 20/40/1 (fights the toolchain); relaxed
50/100/3 (weakens the identity).
REVISIT WHEN: First packaged build measures reality; numbers may tighten.

DECISION: D-026 Engineering principles
STATUS: Confirmed
CHOICE: Seven principles as EDD section 2: assurance honesty above all;
evidence over claims; boring beats novel until novelty pays rent; the fork
is upstream; small is a feature; one owner per fact; every shortcut is
labeled and logged.
BECAUSE: Owner confirmed the starter set unchanged.
OPTIONS CONSIDERED: Owner edits (none requested).
REVISIT WHEN: A principle repeatedly loses arguments it should settle.

DECISION: D-027 Protected goals
STATUS: Confirmed
CHOICE: Three protected when tradeoffs bite: honest and observable; small
and fast (D-025 numbers); accessible (keyboard and assistive tech on
Linux).
BECAUSE: Owner picked all three recommended. Maintainable remains a goal,
carried by the documents system, but yields in conflicts.
OPTIONS CONSIDERED: Maintainable as a protected slot.
REVISIT WHEN: A conflict between the three protected goals themselves
forces a ranking.

DECISION: D-028 Agent stop list
STATUS: Confirmed
CHOICE: Beyond deletions, secrets, and deploys, explicit owner approval is
always required for: new dependencies (NuGet or native); submodule pin
bumps and PRs against the fork; releases, tags, and repo settings; and
schema-class changes to the backfill journal or evidence formats.
BECAUSE: Owner selected all four. Protects the size budget, cross-repo
visibility, outward-facing surfaces, and rollback compatibility.
OPTIONS CONSIDERED: Narrower lists.
REVISIT WHEN: The list blocks work in practice; items may move to
notify-after with a superseding entry.

DECISION: D-029 Definition of done and release approval
STATUS: Confirmed
CHOICE: Template per-change and per-release checklists plus this project's
gates (size budget, evidence links, document updates). Final release
approval rests with Daniel Boyd.
BECAUSE: Owner confirmed as drafted with no scar-tissue additions.
OPTIONS CONSIDERED: Additional checklist items (none named).
REVISIT WHEN: A release retrospective surfaces a missed class of failure.

DECISION: D-030 Client versions and publish mode
STATUS: Confirmed
CHOICE: Avalonia 12.1.x on .NET 10; compiled bindings throughout; NativeAOT
primary publish with trimmed self-contained fallback; X11 default with
native Wayland behind an opt-in flag (Avalonia.Wayland package). Engine
netstandard2.0; shared app core net8.0; Linux app net10.0.
BECAUSE: Avalonia 12 defaults to compiled bindings (NativeAOT-safe), ships
the AT-SPI2 Linux accessibility backend (protected Accessible goal), and
FluentAvalonia 3 supports it. .NET 10 is the recommended runtime. Evidence
in RESEARCH-2026-08-11-unknowns.md.
OPTIONS CONSIDERED: Avalonia 11.3 + .NET 8 (battle-tested, but no AT-SPI2,
opt-in bindings, migration later anyway); Avalonia 12.1 + .NET 8 (no clear
gain).
REVISIT WHEN: The NativeAOT publish spike (A-002 in the EDD ledger) fails;
fallback is the trimmed self-contained mode already named here.

DECISION: D-031 First slice selection
STATUS: Confirmed
CHOICE: SLICE-001 Verify: album verification against AccurateRip and CTDB
with honest verdict, named report, 2026 theme, journal seam with
verification backfill, packaged as .deb and AppImage. Build order M1-M6
with spikes S-1/S-2/S-3 first (SLICE-001-verify.md).
BECAUSE: Proves every architecture seam without drive hardware or native
codecs (managed WAV/FLAC/ALAC decode suffices). Passes the
deserve-to-stop test: verification is CUETools' most-used job.
OPTIONS CONSIDERED: Rip-first (riskiest module first, slower to value,
hardware-bound); convert-first (fewer seams proven at similar cost).
REVISIT WHEN: Slice done with evidence; expansion loop picks the next.

DECISION: D-032 SLICE-001 stubs and exclusions
STATUS: Confirmed
CHOICE: Repair, rip, convert, enrichment backfill, and the native codec
runtime are excluded with named reconnection points. Stubs: honest "codec
unavailable" rows for WavPack/APE/TTA sources; nav shows only built pages;
journal's enrichment lane exists in format, never populated.
BECAUSE: Owner confirmed; nothing pretends to exist, and every exclusion
names its seam.
OPTIONS CONSIDERED: Repair inside slice 1 (bigger boundary, slower proof).
REVISIT WHEN: SLICE-002 selection.

DECISION: D-033 Autonomy grant for SLICE-001 execution
STATUS: Confirmed
CHOICE: Owner authorized (2026-08-11, in session): the agent may merge its
own PRs in cuetools-linux, open and merge the SLICE-001 fork PRs, and
proceed through the approved build order autonomously, stopping only where
genuine owner input is required. D-028 remains the default posture outside
this grant; hard stops remain for anything destructive, scope-changing, or
outside the slice boundary.
BECAUSE: Owner said "feel free to merge yourself" and "continue to work
autonomously until you can't go any further without input from Me".
OPTIONS CONSIDERED: Per-action approval (the D-028 default).
REVISIT WHEN: SLICE-001 closes, or the owner narrows the grant.

DECISION: D-034 Linux-repo restore posture (no lock files)
STATUS: Confirmed
CHOICE: This repository carries no NuGet lock files anywhere in its build
graph. eng/build.sh (used by CI and contributors) passes
RestorePackagesWithLockFile=false and RestoreLockedMode=false on every
build so the fork submodule's lock machinery never engages here.
Determinism comes from exact-pinned package versions in first-party
csproj files plus the extern/cuetools_2026 submodule commit pin. CI
asserts the submodule working tree stays byte-clean after building.
BECAUSE: Measured on 2026-08-11: restoring the fork's engine projects on
Ubuntu modifies their committed packages.lock.json (Linux auto-injects
Microsoft.NETFramework.ReferenceAssemblies for net47; newer NuGet writes
the TagLibSharp project id with different casing), so locked-mode restore
can never pass on a non-Windows host today. Filed upstream as fork issue
LynxTWO/cuetools_2026#7 with the diff evidence. A Directory.Build.rsp
variant of the neutralization failed silently and was rejected in favor of
explicit flags in one script.
OPTIONS CONSIDERED: Fork-side lock invariance now (needs devenv/Windows
validation the Linux side cannot provide; left to the fork via issue #7);
per-OS conditional package references (locked mode then fails on the other
OS); two-SDK split builds (does not remove the OS-conditional injection).
REVISIT WHEN: Fork issue #7 lands OS-invariant locks; this repo then
re-enables lock files and locked CI restore as its own decision.

DECISION: D-035 Test framework for first-party Linux projects
STATUS: Confirmed
CHOICE: xunit (with Microsoft.NET.Test.Sdk) for CUETools.Linux.Tests;
Avalonia.Headless.XUnit planned for UI tests at M4.
BECAUSE: Avalonia's headless testing ships first-class xunit support,
which EDD section 11 already names for page-logic tests. The fork's MSTest
convention stays fork-side; contracts do not cross the repo boundary.
OPTIONS CONSIDERED: MSTest v2 (fork convention, weaker Avalonia headless
support).
REVISIT WHEN: Avalonia's headless MSTest support reaches parity and
consistency with the fork starts to matter.

DECISION: D-036 SLICE-002 selection: Repair
STATUS: Confirmed
CHOICE: SLICE-002 is CTDB parity repair inside the Verify & Repair page
(SLICE-002-repair.md). Owner explicitly authorized starting it while
SLICE-001's two owner-side rows (S-007 theme review, S-008 accessibility)
remain open in parallel; those rows stay owned by the owner and gate
SLICE-001's Done status, not SLICE-002's start.
BECAUSE: Completes the page's named promise with the smallest step: the
engine machinery and command wiring already ship in the shared core;
SLICE-001 omitted only the UI surface (D-032). Repair is CUETools' most
distinctive capability.
OPTIONS CONSIDERED: Codec runtime (unlocks WavPack/APE, closes A-003);
rip foundations (largest lift).
REVISIT WHEN: SLICE-002 closes; the expansion loop reconvenes.

DECISION: D-037 Fork housekeeping authorized
STATUS: Confirmed
CHOICE: Two fork-side PRs approved to run between slice work: engine test
TFM modernization (net8.0 targets for the net47-only test projects, with
test-suites.json lane updates) and the de-reflection seam for the
DoVerify migration (shrinks the Linux app's trimming exemptions).
BECAUSE: Both close findings from the SLICE-001 build (Linux-runnable
engine tests; TrimmerRootAssembly breadth). Windows CI validates both
before merge.
OPTIONS CONSIDERED: Deferring both (keeps slice focus, leaves the loop
slower and the trim surface wide).
REVISIT WHEN: Both PRs merge or CI rejects an approach.

DECISION: D-038 User manual sourcebook
STATUS: Confirmed
CHOICE: Collect user-manual raw material continuously while capabilities
ship (docs/manual/notes/, one plain-English note per topic, referencing
the evidence screenshots), and assemble the full HTML manual after the
port surface stabilizes. Per-release definition of done gains "manual
notes current for user-visible changes" (EDD section 17).
BECAUSE: Owner proposed collecting during the build; capture-now
assemble-late preserves knowledge that is expensive to reconstruct
(verdict vocabulary, honest-behavior rationale) and reuses the evidence
screenshots as illustrations. Notes follow the no-invented-behavior rule:
only verified behavior enters a note.
OPTIONS CONSIDERED: Write the manual at the end from memory (archaeology,
loses nuance); write full manual pages per slice now (churn while the
surface still moves).
REVISIT WHEN: The port surface stabilizes enough to design the HTML
manual's structure; that assembly is its own planned piece of work.

DECISION: D-039 SLICE-002 closed with owner walkthrough approval
STATUS: Confirmed
CHOICE: SLICE-002 (Repair) is Done. All acceptance rows S2-001..S2-006
are evidenced; the owner ran the program against the real repaired
walkthrough on 2026-08-12 and approved. The real-disc run also hardened
the stack: fork PR #13 (source-generated repair receipt JSON so evidence
sealing works under the AOT runtime), the --repair driver's
one-attempt-per-disc guard, and the AvaloniaFact threading rule for
view-model-driving tests.
BECAUSE: The slice's definition of done required evidenced criteria plus
owner walkthrough approval; both now exist.
OPTIONS CONSIDERED: None needed; closure follows the slice brief's DoD.
REVISIT WHEN: The expansion loop reconvenes for the next slice.

DECISION: D-040 SLICE-003 is Convert
STATUS: Confirmed
CHOICE: The owner selected Convert for SLICE-003 on 2026-08-12: the
classic CUETools conversion path (cue or album folder in, re-encoded
tracks out, cue fidelity kept) with the codec picker, encoder settings,
and the CodecScope/ConvertScope visualizations. First increment is
all-managed (Flake FLAC, ALAC, WAV encoders already compiled in). The
owner also granted overnight autonomy: keep working one slice at a time;
owner-facing rows queue for morning.
BECAUSE: Highest user value per effort; no native runtime work blocks a
first increment; builds the encoder-settings surface the codec-runtime
slice later plugs into. The convert closure in the fork is portable
(ConvertService, EncoderCatalog, CodecCatalogModels carry no WPF
dependencies; ConvertViewModel needs only the existing dispatcher seam).
OPTIONS CONSIDERED: Codec runtime (closes A-003, more plumbing); rip
foundations (largest lift); enrichment backfill (smallest).
REVISIT WHEN: SLICE-003 closes; the expansion loop reconvenes.

DECISION: D-041 SLICE-004 is the batch Queue (provisional)
STATUS: Confirmed (owner confirmed 2026-08-13, morning review)
CHOICE: Under the owner's overnight grant ("keep working autonomously one
slice at a time as long as you can", 2026-08-12 night), the agent selected
the batch Queue page for SLICE-004: stack album folders or cue sheets,
choose Verify or Convert per batch, run them in one sitting with honest
per-item status. Owner confirms or vetoes in the morning; this entry
flips to Confirmed or Superseded then.
BECAUSE: It is the strongest dependency-free candidate. The codec-runtime
slice needs new native libraries, a D-028 stop-list item that stays with
the owner even under autonomy. The queue builds directly on the convert
stack shipped hours earlier, serves the mass-convert-a-collection use
case, and its WPF surface (QueueViewModel, 250 lines, one dispatcher
touchpoint) extracts by the established recipe.
OPTIONS CONSIDERED: Codec runtime (blocked on native dependency
approval); enrichment backfill (D-011 extension point, but its apply
flow mutates user files and deserves owner input on the UX); rip
foundations (largest lift, owner-heavy invariants).
REVISIT WHEN: The owner reviews this selection in the morning.

DECISION: D-042 Codec runtime approved with vendored pinned natives
STATUS: Confirmed
CHOICE: SLICE-005 is the codec runtime: native codec libraries (libFLAC,
WavPack, Monkey's Audio) join the Linux app. Sourcing model: vendored
pinned builds - the .so files are compiled from the fork's staged,
pinned vendor sources (the same obj/vendor-sources discipline the
Windows side uses), hash-recorded, and shipped in the packages. Not
distro packages: no version drift underneath the engine's assurance
claims, and the AppImage needs bundling regardless.
BECAUSE: Owner selected vendored pinned builds 2026-08-13, explicitly
approving the new-native-dependency stop-list item (D-028) for this
slice. Byte-pinned provenance matches the project's evidence discipline;
size cost is accepted.
OPTIONS CONSIDERED: Distro packages (smaller, auto-patched, but
unpinned); deferring the slice.
REVISIT WHEN: A security advisory against a vendored codec forces a
faster patch channel than a pin bump.

DECISION: D-043 Settings persistence follows as SLICE-006
STATUS: Confirmed
CHOICE: After the codec runtime, the next slice is settings persistence:
the Linux app keeps output folders, naming scheme, selected codecs, and
encoder settings across launches (the WPF head's SettingsStore role,
implemented against the app-core AppSettings).
BECAUSE: Owner selected it 2026-08-13. Linux currently forgets
everything between launches and the gap grows with every page shipped.
OPTIONS CONSIDERED: Rip foundations (needs the hardware ground-rules
interview first); enrichment backfill (needs the apply-UX
mini-interview).
REVISIT WHEN: SLICE-005 closes.

DECISION: D-044 First public release deferred
STATUS: Confirmed
CHOICE: No v0.x preview release yet; keep building. Releases and tags
remain owner-gated (D-028).
BECAUSE: Owner answered "not yet" on 2026-08-13.
OPTIONS CONSIDERED: Plan now; plan after the next slice.
REVISIT WHEN: The owner says so, or the slice cadence produces a surface
the owner wants public.

DECISION: D-045 Rip evidence matrix is two drives
STATUS: Confirmed
CHOICE: The Linux rip evidence matrix is the laptop's optical drive plus
the 5950X desktop's drive. The desktop drive produces Linux evidence by
being temporarily connected to the Linux laptop (USB enclosure or swap)
for evidence sessions; the desktop stays a Windows machine.
BECAUSE: Owner selected both drives and the move-when-needed mechanism,
2026-08-13. Two drives keep drive-specific quirk handling honest.
OPTIONS CONSIDERED: Laptop-only matrix; live-USB Linux boots on the
desktop.
REVISIT WHEN: The rip slice starts and the second drive's first
evidence session is scheduled.

DECISION: D-046 Rip is full-secure-or-nothing, direct SG_IO
STATUS: Confirmed
CHOICE: No rip capability ships on Linux until the complete WPF
invariant set (calibration, cache defeat, held-state, flagged
vote/retry policy) works there. The backend is direct SCSI (READ CD via
SG_IO) from the first line; no GStreamer interim rip.
BECAUSE: Owner chose the strict staging and the direct backend,
2026-08-13. Only one kind of rip claim will ever exist on Linux, and
the backend is built once.
OPTIONS CONSIDERED: Foundations-first with a plainly-labeled non-secure
rip (D8-B precedent); GStreamer interim backend.
REVISIT WHEN: Practically never; this sets the rip slice's definition.

DECISION: D-047 External command encoders come to Linux soon
STATUS: Confirmed
CHOICE: A Linux curated-encoder slice (lame, oggenc-class ELF builds
with a Linux manifest: pinned sources/archives, hashes, license and
source obligations per encoder) is in scope, planned after settings
persistence (SLICE-006).
BECAUSE: Owner selected "in scope soon", 2026-08-13.
OPTIONS CONSIDERED: Defer until asked; never on Linux.
REVISIT WHEN: The slice is scheduled; each encoder's license review is
its own gate.

DECISION: D-048 Enrichment apply flow is preview-diff per album
STATUS: Confirmed
CHOICE: Enrichment backfill proposals present a before/after diff of
every proposed change per album (tags, artwork, names) with one
approve/reject per album.
BECAUSE: Owner selected it, 2026-08-13. Fast review, no surprise edits.
OPTIONS CONSIDERED: Per-field approval; auto-apply for safe fields.
REVISIT WHEN: The enrichment slice's design phase.

DECISION: D-049 Artwork policy is embed plus folder.jpg with size cap
STATUS: Confirmed
CHOICE: Fetched covers are embedded in tags AND written as folder.jpg,
honoring the existing maxAlbumArtSize cap.
BECAUSE: Owner selected it, 2026-08-13; matches player expectations.
OPTIONS CONSIDERED: Embed-only; folder.jpg-only.
REVISIT WHEN: A size/quality complaint or player-compat finding.

DECISION: D-050 Manual ships as a GitHub Pages site
STATUS: Confirmed
CHOICE: The assembled user manual (D-038) becomes a GitHub Pages site
built from the manual notes, with proper navigation.
BECAUSE: Owner selected Pages over in-repo single-page, 2026-08-13.
OPTIONS CONSIDERED: In-repo single-page HTML shipped with releases.
REVISIT WHEN: Manual assembly begins.

DECISION: D-051 Distro floor is tested, not best-effort
STATUS: Confirmed
CHOICE: The AppImage's older-glibc claim gets real verification (e.g. a
CI or manual check against Ubuntu 22.04 / Debian 12 class glibc), not a
best-effort disclaimer.
BECAUSE: Owner selected the tested floor, 2026-08-13.
OPTIONS CONSIDERED: Ubuntu 24.04-only claims with best-effort AppImage.
REVISIT WHEN: The verification lane is designed (release-adjacent work).

DECISION: D-052 Versioning matches the fork lineage; GPL by repo links
STATUS: Confirmed
CHOICE: When releases start (still deferred, D-044), versions track the
fork's 2.2.x lineage so engine and app read as one family. GPL
source-correspondence is satisfied by release notes linking the exact
tagged app source, fork pin, and vendor submodule pins.
BECAUSE: Owner selected both, 2026-08-13.
OPTIONS CONSIDERED: v0.x preview series; date-based versions; attached
corresponding-source tarballs.
REVISIT WHEN: First release planning (D-044 reopens).

DECISION: D-053 Rip development may use a dev-only diagnostic read
STATUS: Confirmed
CHOICE: A --rip-diagnostic capability, compiled out of release builds
entirely, provides raw labeled reads for calibrating against real drives
and collecting the evidence the secure engine needs. Users can never see
or invoke it; D-046's full-secure-or-nothing shipping rule is untouched.
BECAUSE: Owner selected it 2026-08-13 (rip mini-interview round).
OPTIONS CONSIDERED: No reads of any kind until the secure engine exists.
REVISIT WHEN: The secure engine replaces the last diagnostic use.

DECISION: D-054 The rip slice targets full RipView parity
STATUS: Confirmed
CHOICE: The slice's UI definition of done is the complete WPF Rip page:
live telemetry with CodecScope, release/metadata pickers, artwork, Test
& Copy evidence rows, and the drive panel - built incrementally, but the
slice does not close on a reduced page.
BECAUSE: Owner selected it 2026-08-13; matches the parity ethos.
OPTIONS CONSIDERED: A focused first UI with richness as a later slice.
REVISIT WHEN: Practically never; this defines the slice.

DECISION: D-055 Test & Copy belongs to the first secure milestone
STATUS: Confirmed
CHOICE: The two-pass CRC-compared rip ships with the secure engine, not
after it, so the held-state, phase-evidence, and tie-break invariants
shape the engine rather than being retrofitted.
BECAUSE: Owner selected it 2026-08-13.
OPTIONS CONSIDERED: Single-pass secure first, T&C on top.
REVISIT WHEN: The milestone plan proves T&C must stage later anyway.

DECISION: D-056 The WH16NS40 joins early, during calibration
STATUS: Confirmed
CHOICE: The desktop's HL-DT-ST BD-RE WH16NS40 (the drive the fork's
3E/02 and 08/0A carve-outs were written on) connects to the laptop
during calibration design, so both drives shape the abstractions from
the start. The laptop's own PLDS DVD-RW DU8A5SH (firmware BU51,
/dev/sr0 + /dev/sg1, cdrom group access already in place) is matrix
drive #1. Owner handles the physical logistics and will connect it.
BECAUSE: Owner selected early involvement over a later consolidated
session, accepting the swapping cost to reduce abstraction rework.
OPTIONS CONSIDERED: After the laptop-drive milestone; on-demand timing.
REVISIT WHEN: The second drive is connected (owner will say).

---

DECISION: D-057 Rip progress lives on two surfaces plus the job bar
STATUS: Confirmed
CHOICE: Per-track progress renders in both the track grid (row fill with
an active-track accent) and a new duration-proportional segmented track
strip above the job bar; the job bar itself gains phase chip, mode chip,
and a pass lane. Track boundaries come from the TOC, so Image + Embedded
Cue behaves identically to per-track layouts.
BECAUSE: Redundant on purpose: the strip is glanceable from across the
room, the grid carries detail up close. AI recommendation differed (grid
fill only); owner chose both surfaces.
OPTIONS CONSIDERED: Grid row fill only (AI recommendation); segmented
strip only; track wedges inside the existing DiscReadMap.
REVISIT WHEN: The 1200 px legibility check (S10-007) fails for
high-track-count discs.

DECISION: D-058 Progress visual language: phase by shape, mode by
mechanics, damage by literal ticks
STATUS: Confirmed
CHOICE: Test renders hollow with a TEST chip, Copy solid with a COPY
chip (shape carries the distinction; hue is secondary). A test-completed
track keeps its hollow outline under the later Copy fill (phase memory,
mirroring the immutable phase-evidence rule). Mode shows as a chip plus
a job-bar pass lane with literal pass ticks; Burst has no lane. Strip
segments whose sectors needed rereads gain an amber edge tick with
literal counts in the tooltip; grid rows stay pure progress. Terminal
states are two: clean, and completion-with-unrecoverable (red edge,
literal failed count). Routine corrections do not mark.
BECAUSE: Colorblind-safe by construction, and every displayed element
maps to a real engine event or literal counter, per the standing
honesty rules of the rip visuals.
OPTIONS CONSIDERED: Hue-only phase distinction; stacked phase lanes;
pattern fills; mode color themes; per-pass fill texture; three-state
terminals including corrections; damage heat-mapping on the strip.
REVISIT WHEN: Live evidence shows the language misreads on real runs.

DECISION: D-059 SLICE-010 boundary: Linux controls over shared VM state
STATUS: Confirmed
CHOICE: Track-progress state, phase, mode, and per-track counters land
in the shared RipViewModel (fork App.Core) with unit tests; the two new
controls and job-bar enrichment are Avalonia-only this slice. WPF
parity is a named extension point: port the two controls, the VM
already speaks the language.
BECAUSE: Both heads inherit the state for free; UI work stays scoped to
the head that can produce evidence on this machine.
OPTIONS CONSIDERED: Both heads in one slice.
REVISIT WHEN: The WPF parity slice is selected.

---

DECISION: D-060 Drive recovery is guided and physical; the app never
resets hardware
STATUS: Confirmed
CHOICE: The stuck-drive recovery dialog walks the user through physical
rungs only: USB cable replug, then power cycle, each with a visual and
each verified live by the app (re-enumeration watch plus an
unprivileged TOC probe). No software reset rungs, no sudo helpers, no
elevation.
BECAUSE: Live characterization (fork finding doc, 2026-08-14) proved
every host-side reset useless on real wedged hardware - SCSI device
reset, host reset, USB port reset, even a full cable replug with power
maintained - and all software rungs need privileges the app does not
have. The owner proposed auto-run software rungs; the evidence removed
them, and the owner accepted the physical-only refinement.
OPTIONS CONSIDERED: Auto-run software reset ladder (owner's first
sketch); physical-only guided ladder (chosen); message-only status quo.
REVISIT WHEN: Any drive is observed curing on a host-issuable reset.

DECISION: D-061 Per-drive incident memory with lead-with-known-cure
STATUS: Confirmed
CHOICE: Each wedge incident is recorded per drive identity (timestamp,
trigger-context counters, rungs attempted, curing rung). After the same
rung cures twice consecutively for a drive, the dialog leads with that
rung and offers "skip to what worked before"; the full ladder stays
reachable. Records carry hardware identity and counters only.
BECAUSE: Mirrors the drive-calibration precedent of persisting proven
per-drive facts. Owner proposed learning after three occurrences; the
two-consecutive-cures refinement was accepted (the rungs are human
actions, so only the order changes and nothing is locked out).
OPTIONS CONSIDERED: Fixed three-strike threshold (owner's first
sketch); two consecutive cures with full ladder reachable (chosen); no
memory.
REVISIT WHEN: Incident data shows drives alternating cures.

DECISION: D-062 A cured drive gets a fresh run, never a resumed one
STATUS: Confirmed
CHOICE: After a verified cure the dialog offers "Retry now," which
starts a new operation through the normal calibrated paths. The failed
transaction stays failed. An uncured ladder ends in an honest terminal
state (different port or cable, possible service) and is recorded as
uncured. The wedge signature also gains surfacing from ordinary
payload-read storms, not just cache defeat, before the dialog ships.
BECAUSE: The failed operation's secure-independence was already
unprovable; fail-closed doctrine holds. Detection breadth keeps the
dialog reachable from every place the wedge can actually appear.
OPTIONS CONSIDERED: Auto-resume the interrupted operation; resume with
a re-verification pass; fresh run only (chosen).
REVISIT WHEN: Never expected; supersede explicitly if doctrine changes.

DECISION: D-063 SLICE-009 closed; two evidence rows transfer to
SLICE-011
STATUS: Confirmed
CHOICE: SLICE-009 (rip) is Done with evidence, signed off 2026-08-15
after the owner's extended live use of the rip surface across three
drives, two published clean sets, and the damaged-disc session. The
tie-break-third-read and Held-state-UX rows transfer to SLICE-011's
live-evidence session as S11-007/S11-008.
BECAUSE: Both rows need a ~50 minute damaged Test & Copy with Stop off
on hardware that has wedged twice at ~24 minutes; inside SLICE-011's
live session a mid-grind wedge is the recovery dialog's own test case
instead of a lost run.
OPTIONS CONSIDERED: Chase the rows the same night; leave the slice
open.
REVISIT WHEN: SLICE-011's live session closes (rows must be banked or
explicitly re-dispositioned there).

DECISION: D-064 Next slice is SLICE-011 guided drive recovery
STATUS: Confirmed
CHOICE: SLICE-011 is approved for build, selected over SLICE-010
(progress visuals, stays Proposed and queued) and a settings page
(candidate, needs its own mini-interview).
BECAUSE: It completes the D11 story while the wedge knowledge is
fresh, its mandatory live session doubles as the harvest for the
transferred SLICE-009 rows, and most of it is testable without
hardware.
OPTIONS CONSIDERED: SLICE-010 progress visuals; settings page; pause
building.
REVISIT WHEN: SLICE-011 closes (SLICE-010 and the settings page are
the standing candidates).

DECISION: D-066 The recovery probe lives in the fork's App.Core, not the
Linux app
STATUS: Confirmed
CHOICE: IDriveRecoveryProbe, its Linux implementation, and the ladder
state machine live in CUETools.App.Core in the fork. The brief's
section 6 placed the watcher and TOC probe in the Linux app.
BECAUSE: The drive-letter to sr-node mapping already exists twice in the
fork, with comments in both places asking that the copies be kept in
agreement (OpticalDriveLease and CUETools.Ripper.CDDrivesList). A third
copy in a repository that cannot see the other two is exactly the drift
this project exists to prevent. ARCHITECTURE.md section 5 also requires
the interface in the fork regardless, and App.Core is plain net8.0, so
the ladder is testable from both suites. PlatformInterfaces.cs was
rejected as the home: it is a toolkit seam file whose single organizing
idea is that the core never names a UI type, and a sysfs read names no
UI type but is not a toolkit either.
OPTIONS CONSIDERED: Linux app as the brief said; PlatformInterfaces.cs;
fork App.Core in its own file (chosen).
REVISIT WHEN: The owner rules, or WPF parity needs a different split.

DECISION: D-067 A permissions failure records no incident
STATUS: Confirmed
CHOICE: When the probe cannot open the device for lack of permission,
the ladder stops in its own terminal state and writes nothing to the
incident store.
BECAUSE: A permissions failure means no rung was ever tested. Recording
it would write an incident with an empty curing rung, and an uncured
incident breaks a drive's proven-cure streak, so a permissions problem
would silently erase what the drive had already taught us.
OPTIONS CONSIDERED: Record it as uncured; add a field distinguishing it
(rejected: the incident type has no such field and adding one changes a
persisted contract).
REVISIT WHEN: Live evidence shows users hitting permissions often enough
that the absence of a record hurts diagnosis.

DECISION: D-068 The drive claim covers each probe, not the user's
physical action
STATUS: Confirmed
CHOICE: The probe claims the drive before each TOC read and releases it
immediately after. It does not hold the claim across the human rung
(unplugging a cable, cutting power).
BECAUSE: Two halves. Claiming at all is required - the probe opens the
device and a TOC read moves the head, so probing a drive another window
is ripping would corrupt that job's evidence; the CLAUDE.md rule to
claim before querying identity applies. Not holding across the physical
action is the departure from the same rule's "for the complete
operation" wording: the lease is a file lock, not a device handle, so
device disappearance never invalidates it. Holding it through an unplug
would protect nothing while blocking the returning drive's legitimate
owner, and both lease keys go stale anyway because the drive can return
at a different node.
OPTIONS CONSIDERED: Hold across the whole ladder (matches the wording,
protects nothing, blocks a real owner); no claim at all (rejected: can
corrupt another window's rip).
REVISIT WHEN: The live session shows a claim conflict in practice.

DECISION: D-065 First Linux preview release waits for SLICE-011
STATUS: Deferred
CHOICE: The rip-slice completion trigger fired (D-063), and the owner
chose to hold the first public preview until SLICE-011 lands, so the
release ships with guided recovery rather than a known-unguided wedge
behavior on USB drives.
BECAUSE: Owner call 2026-08-15; supersedes the "after the rip slice
completes" timing from 2026-08-14.
OPTIONS CONSIDERED: Prepare the release immediately; revisit later
with no named trigger.
REVISIT WHEN: SLICE-011 section 11 closes with evidence.

DECISION: D-069 CTDB submission is asked once and remembered
STATUS: Confirmed
CHOICE: The first eligible disc after a completed verify or a published
rip raises a consent dialog with a remember checkbox, writing
`advanced.CTDBSubmit` and clearing `advanced.CTDBAsk`. Both keys already
round-trip through the Linux settings store, so no settings format
change is needed. Nothing is uploaded before an explicit yes.
BECAUSE: Owner call 2026-08-16. Matches the classic Windows heads
exactly, which keeps one consent model across every CUETools surface.
OPTIONS CONSIDERED: Explicit per-disc button only (rejected: nobody
finds it, so the contribution never happens); ask every time (rejected:
becomes click-through noise on a batch); rips only (rejected: loses most
of the volume, since verified albums from elsewhere are the unknown
pressings worth contributing).
REVISIT WHEN: A settings page gives the answer another home, or feedback
says the prompt is unwelcome.

DECISION: D-070 Only rips without unrecoverable errors may be submitted
STATUS: Confirmed
CHOICE: Salvaged output, held Test and Copy results, and rips carrying
unrecoverable windows are not eligible to submit. Eligible discs send
the classic quality value of 100.
BECAUSE: Owner call 2026-08-16. Known-suspect audio must not enter the
database claiming perfect quality, which is what classic's unconditional
`quality: 100` allows today.
OPTIONS CONSIDERED: Full classic parity (rejected for the reason above);
confirmed discs only (rejected: contributes almost nothing, since
unknown pressings are exactly the unconfirmed ones).
REVISIT WHEN: A real quality value is computed from rip evidence instead
of a constant. That follow-up is expected and is carried in SLICE-012.

DECISION: D-071 The engine learns to read .m3u8
STATUS: Confirmed
CHOICE: `CUESheet` widens its exact `.m3u` extension comparisons to
accept `.m3u8`, so the extension that discovery already accepts and the
file picker already offers becomes real.
BECAUSE: Owner call 2026-08-16. Discovery and the picker accept `.m3u8`
today while the engine recognises only `.m3u`, so the app offers a file
it then fails to open.
OPTIONS CONSIDERED: Remove `.m3u8` from discovery and the picker
(rejected: takes a capability away rather than delivering it).
REVISIT WHEN: A playlist format needs handling beyond the extension, for
example encoding rules that differ between the two.

DECISION: D-072 The manual ships as a human layer over the engineering notes
STATUS: Confirmed
CHOICE: `docs/manual/pages/` holds the manual a user reads, written from
a task outline and fact-checked against source. `docs/manual/notes/`
stays the engineering record. The generator publishes a page when one
exists and falls back to its note with a banner. `VOICE.md` records the
wording rules, and conflicting or unverified claims go to
`needs-verification.md` rather than into print.
BECAUSE: Owner call 2026-08-15, after an external review found the notes
read as a defence of the software rather than help for a reader. The
first rewritten page's fact check then found 25 real defects, most of
them inherited from the notes, which settled the "rewrite from source,
never polish the note" rule.
OPTIONS CONSIDERED: Polish the notes in place (rejected: preserves the
engineering skeleton and its errors); one layer only, replacing the
notes (rejected: the receipts are worth keeping).
REVISIT WHEN: Every note has a rewritten page and the fallback path is
dead code.

---

## Slice growth tally

Running list of confirmed decisions that enlarge the initial slice concept.
Presented in full at Phase 5 slice selection.

1. D-010 deterministic backfill: adds a backfill journal concept and a
   pending-verification lane beyond plain degraded-offline behavior.
   D-011 confirms the verification lane lands in v1.
2. D-012 extract-first: the fork must gain a platform-neutral shared app
   core before the Linux shell can show its first real page. Front-loads a
   governed refactor of CUETools.Wpf into the build order.
3. D-023 theme port: the first visible slice carries real styling work; a
   stock-theme page does not meet the identity bar.

DECISION: D-073 Settings page shape (owner interview 2026-08-18)
STATUS: Confirmed
CHOICE: Four calls, all the interview's recommended options. (1) Changes
apply immediately and the file still writes once at exit, so D-043 is
untouched; jobs keep freezing their options at start. (2) Settings is a
rail page under SESSION, not a modal. (3) The Linux-only settings live in
a named "Privacy & data" group: both consent re-arms (artwork lookup,
CTDB sharing), log retention with the default-off keep-forever archive,
and links to what-gets-sent. (4) Secondary drive windows hide Settings
entirely; the primary window owns the profile and the contract stays
impossible to violate.
BECAUSE: Owner answers 2026-08-18, delegating detail to the recommended
options. Immediate apply matches the theme button's existing behaviour;
a rail page keeps the 1200 px width budget and adds no modal stacking;
a named privacy group is findable by the user who wants to change a
remembered consent, which is the strongest motivation for the page
existing now; hiding in secondary windows removes rather than explains
a rule.
OPTIONS CONSIDERED: Apply/Revert staging; save-on-change; a WPF-style
modal; a header-gear overlay; spreading privacy items across feature
groups; read-only settings in secondary windows.
REVISIT WHEN: A setting arrives that is dangerous to apply live, or the
process-per-drive model changes who owns the profile.

DECISION: D-074 Scaling design intake and strategy (owner interview 2026-08-20)
STATUS: Confirmed
CHOICE: The fractional-scaling design covers both heads at 100 through
200 percent, is designed and built on the Linux head first with the
Windows port following on its lessons, and its core strategy is reflow
then scroll: pages restack as logical space shrinks (two columns become
one, the rail narrows), and only below a hard floor does scrolling
appear. Instrument visuals shrink proportionally but each declares a
minimum legible size, below which the page reflows around the
instrument instead of shrinking it further (the S10-007 legibility rule
extended to every instrument). Tier-one scale-proofing covers the Rip
and Verify pages: designed and render-verified at all five factors in
both themes before the slice closes; other pages get the reflow
mechanics with lighter verification. The compact low-resolution mode is
Deferred: no real target machine exists, so it waits with a written
revisit trigger (the day a small-screen machine enters the owner's
world) rather than being designed against a guess. Build starts as soon
as the owner approves the brief.
BECAUSE: Owner answers 2026-08-20, intake restatement confirmed as
given. Reflow-then-scroll does the most work at every size and matches
the Rip page's existing bounded-layout rules; proportional-with-floors
keeps the honesty commitments (strip legibility, damage ticks, CRC
evidence) explicit instead of letting instruments clip silently; Rip
and Verify are where clipped controls cost most, with a drive spinning.
OPTIONS CONSIDERED: Fixed layout with scrollbars; uniform shrink;
fixed instrument sizes; simplified small-mode instrument variants;
all-work-pages or all-eleven-pages tier one; both heads in one pass;
compact mode designed speculatively.
REVISIT WHEN: A real small-screen machine appears (reopens compact
mode), or the Windows port begins (imports these decisions with fresh
evidence).

DECISION: D-075 The rail collapses to an icon strip when width is tight
STATUS: Confirmed
CHOICE: Below the width threshold the nav rail collapses to a narrow
icon strip, one icon per page, tooltips carrying the full names. This
commits the project to designing an icon language it does not yet have:
eleven rail entries need icons that read in the 2026 bench identity,
in both themes, at strip sizes.
BECAUSE: Owner choice 2026-08-20. AI recommendation differed: the
interview recommended auto-narrowing with short text labels to avoid
the icon-design project; the owner chose the icon strip, which saves
the most horizontal space and can share its design language with the
planned analog-controls pass (etched, backlit legends). The extra work
is accepted, not accidental.
OPTIONS CONSIDERED: Auto-narrow with short labels (recommended);
collapsible drawer behind a hamburger button.
REVISIT WHEN: The icon set proves illegible at strip sizes in render
checks, or the analog-controls pass changes the visual language the
icons must speak.

DECISION: D-076 Scaling breakpoints, icon language, evidence, build order (owner interview 2026-08-20)
STATUS: Confirmed
CHOICE: Four calls, all the interview's recommended options. (1) Two
breakpoints and a low floor: the full layout holds at 1140 logical
pixels of width and above; below that the rail collapses to the D-075
icon strip and two-column pages stack to one; below 860 the floor
applies - horizontal scrolling instead of clipping, and the window
minimum drops from 960x560 to 640x480 so the app can fit the 960x540
logical desktop a 200 percent 1080p screen offers. (2) The strip icons
are designed once, in the analog language: a column of small backlit
keys with etched icons, the active page lit; the planned analog
controls pass inherits this language rather than designing a second
one. (3) Tier one closes on an automated render matrix (5 scale
factors x 2 themes x 3 layout states per page, assertion-backed
no-clipping checks) plus one live GNOME fractional-scaling walkthrough
by the owner on real hardware. (4) The icon set is the scaling slice's
first build item, so the strip ships looking final; the analog
controls pass follows as its own slice.
BECAUSE: Owner answers 2026-08-20. The floor numbers come from
measured desktop arithmetic (200 percent on 1080p leaves less logical
height than today's window minimum); one icon language avoids
designing the same eleven-entry set twice and gives the analog pass a
head start; the S10/S11 close pattern (automation plus a live session)
is the house evidence standard.
OPTIONS CONSIDERED: Three gradual breakpoint steps; owner-supplied
thresholds; plain monoline glyphs now with an analog restyle later;
monogram placeholders; automated-only or live-only evidence; reflow
before icons; the full analog pass before any reflow.
REVISIT WHEN: Render evidence shows a breakpoint number producing a
broken intermediate state, or the icon set proves illegible at strip
size (reopens D-075's options).

DECISION: D-077 House voice: warmth, never manufactured errors (owner interview 2026-08-20)
STATUS: Confirmed
CHOICE: Everything written that is not code gets a deliberate human
warmth, adapted from the owner's Internet Human Mode skill with its
dial recalibrated for durable documents: the levels are kept, the
manufactured errors are not. Nothing in these repositories fakes
typos, dropped apostrophes, or rushed typing to look human; humanity
comes from voice (contractions, direct address, varied rhythm, plain
verbs, honest humor), never from damage. The surface map: manual
pages and glossary at level 1 (warm precision, zero errors);
engineering records (ARCHITECTURE, ENGINEERING, DECISION-LOG, slice
briefs, findings) at level 1 with receipt discipline untouched;
README, release notes, and pull-request prose at level 2; assistant
reports to the owner at level 2; the How a CD Works manual page at
level 2-3, because a lesson may carry personality a reference should
not; commit messages unchanged (already governed); code comments
exempt (the file's idiom wins). Emoji and the skill's comic
archetypes stay out of the repositories entirely. Codified in three
layers so it binds future sessions: a house-voice skill in the fork's
.claude/skills, warmth rulings in the manual's VOICE.md, and an
amendment to the fork CLAUDE.md writing rules. Implementation runs
golden-sample-first: one reference page and the lesson page rewritten
and approved before the full sweep.
BECAUSE: Owner request 2026-08-20, level choice delegated to the
assistant and set at 1-2 (3 ceiling for the lesson). The skill's own
guidance marks levels 1-3 as the band where credibility still
matters; a manual with deliberate misspellings breaks search, quotes
UI strings wrong, and spends the trust the receipts earned; and
injected errors in prose the assistant writes would fake provenance,
the arms race the skill's demo post argues against.
OPTIONS CONSIDERED: Real roughness with visible typing texture
(declined); level 1 flat everywhere; per-surface markup by the owner;
codifying in fewer layers.
REVISIT WHEN: A community surface appears where real internet texture
genuinely fits (a social account, a forum presence), which reopens
the upper dial for that surface only.

DECISION: D-078 SLICE-013 approved; the writing pass runs first
STATUS: Confirmed
CHOICE: The owner approves SLICE-013 fractional scaling as written.
Sequencing: the D-077 writing pass lands before the slice's build
begins; within the slice, the icon set remains build item one.
BECAUSE: Owner answers 2026-08-20 ("it looks good", stamp-now option
chosen with the writing pass explicitly queued ahead).
OPTIONS CONSIDERED: Holding the stamp until the brief was re-read in
the new voice.
REVISIT WHEN: Nothing pending; the stamp stands.

DECISION: D-079 SLICE-014 analog controls approved
STATUS: Confirmed
CHOICE: The owner approves SLICE-014 as written (2026-08-21): lamp
checkboxes, the machined key as the default button, transport legends
on the RUN group with disabled-as-unpowered, render-zoo evidence, and
the owner's live eyeball as the close. Combos and spinners stay out;
WPF parity rides the port lane later.
BECAUSE: Owner stamp on the drafted brief, concept board approved
2026-08-20.
OPTIONS CONSIDERED: In the brief.
REVISIT WHEN: The eyeball raises findings.

DECISION: D-080 Soft-body rubber keys: scope and fidelity (owner interview 2026-08-21)
STATUS: Confirmed
CHOICE: Five calls from the 2026-08-21 interview, after an 11-agent
research fan-out with compiled probes on the real GPU.

(1) SILHOUETTE. The rim stays pinned for the first build: the corner
dip reads through shading, tilt, shadow asymmetry and interior
geometry rather than by the outline physically crossing below its
neighbours. The owner looks at it and escalates to a bounded un-pin
only if the shaded dip does not read as "below the surface" to him.

(2) LABEL. The glyphs DO shear with the rubber. See D-081 for the
measurement that made this affordable; it was not affordable under the
first research package's reading.

(3) HOVER. Press-only geometry in the first build. Hover keeps a
non-geometric cue. This is a deliberate subtraction from what the
owner asked for, taken because a hover dimple trails a normal 400 px/s
pointer sweep by 5.8 px (1.29x its own radius), because 15 buttons
carry tooltips so hover is when a user is reading rather than acting,
and because a pointer sweep across the Rip page's 9 visible buttons
drags a wake of settling animation over the one screen where a stall
reads as trouble. Revisit once the press physics ship and the mid-rip
frame budget is measured.

(4) SCOPE. All 61 Button sites plus the 10 RailStripKey navigation
keys. Avalonia's bare Button selector matches by exact type, so
styling Button alone would have left the most-clicked surface in the
app the only thing that stopped moving.

(5) COST POSTURE. The owner stated he does not mind extra build cost
to do this the right way. Recorded because it is load-bearing: it is
why the label shears at all, and it is NOT a licence to take the most
expensive option where the expensive option measures worse.
BECAUSE: Owner answers 2026-08-21, each against measured evidence
presented in the question.
OPTIONS CONSIDERED: Un-pinned rim now, or permanently pinned; rigid
label, or shear only on transport keys; hover as asked, or hover only
on transport keys; transport-only scope, or all buttons without the
rail.
REVISIT WHEN: The owner sees the shaded dip and wants the outline to
move (reopens 1); the press physics ship and hover is re-costed
(reopens 3).

DECISION: D-081 Sheared glyphs are affordable, and why the first answer was wrong
STATUS: Confirmed
CHOICE: Button labels shear with the deforming rubber, using this
exact recipe and no other: a resting key is drawn NATIVELY and the
mesh exists only while the key is actually deforming; the pressed
texture is an SKImage rasterised at 2x device scale for scalings at
and above 1.50x and at device resolution below that; it is sampled
with plain SKFilterMode.Linear, never a cubic resampler and never
SKShader.CreatePicture; it is rasterised once per key per visual state
per scale and re-sampled per frame, never rebuilt per frame; and the
label is drawn into the texture with a hairline 0.06 DIP StrokeAndFill
embolden.
BECAUSE: The first research package measured sheared glyphs as costing
20-24 percent of label edge sharpness at every scaling above 1.0x and
called the obvious mitigation ineffective. The owner asked whether
supersampling the glyphs would fix it. Measuring his question directly
found that the earlier mitigation test was a NO-OP: the three
"recorded at higher resolution" variants render BYTE-IDENTICAL (max
channel delta 0 at every scaling, at rest and pressed), because
SKShader.CreatePicture carries no resolution at all - it replays
vector commands at destination size. His mechanism had never actually
been tested. Rasterising to a real SKImage at 2x and sampling it puts
label sharpness at or above a native draw (total-variation ratio 1.059
/ 1.030 / 1.015 / 1.000 / 0.999 across the five scalings) with mean
error falling from 26.03 to 2.89 at 2.0x.
Three further measurements shaped the recipe. Cubic resamplers RING:
Mitchell puts 10.3 to 15.1 percent of label-band pixels outside the
local range of a correct render (CatmullRom 13.4 to 19.3), while plain
bilinear on the same texture holds 0.0 to 5.4 percent and is both
lower-error and cheaper. Supersampled glyphs come out about 11 percent
lighter than native ones at any resampler, traced to Skia's glyph-mask
gamma boost rather than to filtering (a filled path downsamples to
100.5 percent ink; glyph masks to 88.5), which the embolden pays back
to 99.9-100.9 percent. And a device-resolution texture is
BYTE-IDENTICAL to a native draw at rest at every scaling, which is
what makes the rest-native/press-mesh split free.
OPTIONS CONSIDERED: Rigid label with in-plane pull (the first
package's recommendation, and the fallback the owner named if
supersampling failed); 2x with Mitchell or CatmullRom; output
supersampling; picture-shader recording at DIP or device scale.
REVISIT WHEN: The fidelity check on the REAL Avalonia window surface
finds the native path using LCD subpixel text that a transparent-backed
texture cannot reproduce. That check is a build gate, not a follow-up.

DECISION: D-082 Project skills are invisible to sessions run from the Linux repo
STATUS: Confirmed 2026-08-23. Option (a). See the resolution below.
CHOICE: Measured 2026-08-21: the fork's
.claude/skills (house-voice, lit-panel-controls, codec-visualization,
disc-read-visualization) are NOT discovered by a session whose project
root is the Linux repo, even though the submodule checkout at
extern/cuetools_2026/.claude/skills contains them byte-identically.
Skill discovery keys off the session's project root; a nested
.claude/skills is not scanned, and --add-dir does not change it.
This silently defeats two recorded decisions: D-077 codified the house
voice as "a house-voice skill in the fork's .claude/skills", and ADD
section 8.1b names the visualization skills as the portable patterns
the Avalonia port follows. Both assume the fork's skills bind work done
on the Linux head. They do not, and have not.
OPTIONS: (a) a .claude/skills at the Linux repo root, symlinked to the
fork's (one source, but a checked-in symlink into a submodule path);
(b) copy them (drifts); (c) promote the shared ones to the user-level
~/.claude/skills (works everywhere, measured, but leaves version
control); (d) require cross-head UI work to run from the fork
(cheapest, unenforced, and it just failed silently).
BECAUSE: Surfaced by the SLICE-015 research. Recorded rather than
fixed because the fix is the owner's call about where shared knowledge
lives.
RESOLUTION 2026-08-23: option (a), the symlink. Two things were
measured before choosing, because the original entry only established
what does NOT work.
(1) A .claude/skills at the LINUX repo root IS discovered. Tested with
a throwaway probe skill: it appeared in the session listing. So the
discovery rule is "project root only", not "no nested directories" -
the fork's copy fails because it is nested, not because submodule
paths are special.
(2) A relative symlink is followed. `.claude/skills ->
../extern/cuetools_2026/.claude/skills` surfaced all four fork skills
in a live session. Git stores it as one 120000 blob, so there is one
source of truth and no copy to drift.
The objection recorded in option (a), that it is a checked-in symlink
into a submodule path, turns out to be cheap. An uninitialised
submodule leaves the link dangling, and discovery then finds nothing
and reports nothing: it degrades to today's behaviour rather than
erroring. Windows symlink support does not apply, because this is the
Linux and macOS head.
Because the link points at the DIRECTORY, a skill added to the fork
needs no wiring here. It appears on the next submodule pin bump. The
soft-body-controls skill that forced this decision is the first case:
it lands with the pin, not with a second edit.
Rejected: (b) copying drifts, which is the failure this decision
exists to prevent; (c) promoting to ~/.claude/skills leaves version
control, so the skills stop being reviewable with the code they
describe; (d) "run cross-head work from the fork" is the unenforced
convention that already failed silently once.

DECISION: D-083 Why bilinear wins, and why RIOT's Mitchell does not transfer
STATUS: Confirmed
CHOICE: D-081's recipe is unchanged: a 2x device-scale texture sampled
with plain SKFilterMode.Linear. But its REASONING is corrected, and
one of its stated virtues was wrong.
BECAUSE: The owner challenged the anti-Mitchell finding with hands-on
evidence: RIOT's Mitchell-Netravali downscaler gives him visibly clean
results with no blur and no ringing on large-factor photo downscales.
He was right about the mechanism, and measuring it explained a result
the audit had only described.
Skia's cubic sampler uses a FIXED texel footprint that does not scale
with the minification ratio. Measured by sliding a one-texel impulse
past a fixed output pixel: Skia Linear and Skia Mitchell both touch 2
source texels at every ratio from 2:1 to 8:1, while a properly scaled
software Mitchell touches 3, 4, 7, 8, 14, 18. On a source-Nyquist
grating whose only correct answer is flat grey, Skia's samplers pass
it through at near full contrast (sd 76.4 at ratio 1.80, 63.5 at
2.50) and read exactly 0.0 only at 2.00. So plain bilinear is correct
at ONE ratio, where its 2x2 taps happen to be the exact box, and
Skia's cubic rings because its negative lobes sit across strokes at a
footprint that never widens. That is the cause the audit was missing.
The fix does not transfer, and measures worse on every axis. Averaged
over 24 press frames at 1.50x, each judged against its own 8x
brute-force reference: the current recipe scores rmse 5.05 with edge
energy 1.079 and 0.2 percent ringing, while a software
Mitchell downscale to device resolution scores rmse 12.76, edge energy
0.614 and 2.4 percent ringing. At rest the gap widens (2.22 versus
7.26, ringing 0.1 versus 5.7 percent). Two measured reasons. First,
our source is vector art, not a photograph: a 2x raster IS the exact
area integral of the ideal image over each 2x pixel, so the correct
2:1 reduction is the BOX and anything wider is simply wrong (software
box at rest scores 2.23 from the identical source, Mitchell 7.26).
RIOT's Mitchell wins on photos because there the correct answer is
undefined and its passband boost reads as sharpness; here the correct
answer is defined. Second, downsampling to device resolution destroys
the sub-pixel positioning the warp needs: the box is exact at rest
(0.997 edge energy) but collapses to 0.757 under press, while a 2x
texture holds near 1.0 because the warp's fractional displacement is
absorbed inside a footprint already two texels wide.
CORRECTION TO D-081: it recorded 2x+Linear keeping "109 to 116 percent
of the reference's label edge energy" as a virtue. On a confound-free
key the correct target is 100 percent and the recipe measures 107.9
percent press-averaged. That excess is mild aliasing, not fidelity.
The recipe is right for a different reason than stated.
A confound was also found and removed: Skia grid-fits glyph outlines
to whatever raster size it is asked for and pushes masks through a
size-dependent contrast LUT, so a 2x raster is not a scaled copy of a
1x one. Drawing the label as a filled path with hinting None makes it
scale-invariant and drops the device-res rest error from rmse 14.6-38.2
to 3.65-4.97. Most of what the earlier tables measured was glyph
grid-fit, not resampling.
OPTIONS CONSIDERED: software Mitchell, B-spline, Catmull-Rom, Lanczos3
and box downscales at 2x, 3x and 4x source; Skia cubic samplers; a
custom mip chain (impossible: mips are box-built, no public API to
install one, and DrawVertices never tells the sampler the local scale
varies, measured as a difference of 1 across mesh scales 0.80 to 1.50).
REVISIT WHEN: quality is wanted beyond the current recipe. The
direction is OUTPUT supersampling, never texture downsampling: render
the warped mesh to a 2x offscreen and box it down. With the existing
2x texture that is rmse 5.05 -> 3.70 for 3.0x frame cost; with a 4x
texture, where the offscreen ratio becomes exactly 2:1 and bilinear is
again the exact box, it is rmse 5.05 -> 2.46, the measured ceiling,
for 3.9x frame cost and 4x texture memory.

DECISION: D-084 Three silent failures that must not be "fixed" back
STATUS: Confirmed
CHOICE: Recorded before any soft-body code is written, because each
one fails with no error, no warning and no test failure, and each
would look like a bug worth reverting to someone who did not measure
it.
(1) A TransformOperationsTransition on RenderTransform FLATTENS a
perspective matrix to its affine part. Measured: a Border carrying the
transition reports TransformOperations with M13 = 0 and perspective
false, while the same Button with an empty Transitions collection
reports a MatrixTransform with perspective true. Both the app's own
transition in AnalogControls.axaml and FluentTheme's must be removed
for a projective tilt to survive. Nothing warns.
(2) A locally set RenderTransform OUTRANKS the :pressed style trigger.
Once the behaviour assigns a transform in code, the existing
translateY(1.2px) depression silently stops firing while the XAML
still reads as though it works.
(3) If the key face ever becomes self-drawing, every future selector
of the form `Button.something /template/ Border#keyFace` will parse
clean, load clean, match the element, and paint nothing.
BECAUSE: All three were measured during the SLICE-015 research. Each
is invisible to the compiler, to the XAML loader, and to the suite.
REVISIT WHEN: Never as a "cleanup". Any change here needs its own
measurement.
