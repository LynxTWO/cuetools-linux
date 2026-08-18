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
