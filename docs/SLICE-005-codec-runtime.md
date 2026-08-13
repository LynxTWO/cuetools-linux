# CUETools Linux Slice Brief: SLICE-005 Codec Runtime

Version: 0.1. Date: 2026-08-13. Status: Approved for build (owner selected
2026-08-13, D-042, explicitly approving the vendored-native dependency
model under the D-028 stop list).
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md,
SLICE-003-convert.md, SLICE-004-queue.md.

## 1. What the slice adds, and why it is next

- **Capability:** the native codec runtime: libFLAC, WavPack, and
  Monkey's Audio join the Linux app as vendored, pinned shared libraries
  built from the fork's own staged sources. Verify, repair, convert, and
  queue gain .wv and .ape coverage plus the native FLAC implementation
  beside the managed Flake.
- **Why now:** owner selected it (D-042). It closes spike A-003 and is
  the last capability gap between the Linux app's audio engine and the
  Windows head's in-process codec set (WMA excluded by D-006).

## 2. The trust model (the slice's core)

Vendored pinned natives, never distro packages (D-042):

1. `eng/build-native-codecs.sh` builds the three `.so` files from pinned
   inputs only: the staged, patched vendor trees for FLAC and WavPack
   (the same `obj/vendor-sources/current` closure every other consumer
   uses), and the pinned MAC 13.20 SDK archive with the fork's
   hash-pinned MACLibDll wrapper override and the same `EXCLUDE_CIO`
   define the Windows build uses. It records each library's SHA-256 in
   `native-codecs.json`.
2. The app's `NativeCodecLoader` is the Linux counterpart of the Windows
   PluginTrustManifest for these libraries: every packaged `.so` is
   hash-validated against the packaged manifest BEFORE its exact
   absolute path is registered with `NativeDependencyPathRegistry` and
   loaded. The `DllImportResolver` binds wrapper imports to the
   validated, already-loaded handles; a known codec name that failed
   validation throws instead of falling back. There is no app-root,
   PATH, or bare-name search anywhere.
3. Codec readiness honesty: a codec whose library fails validation is
   simply not registered - never a selectable lie - with the reason
   logged per library.

Fork seams (PR #19, merged): cross-platform `NativePreload` (kernel32 on
Windows, libdl elsewhere), `.so` names in the registry plus
`RegisterHostValidatedPath` with documented host obligations, wrapper
static constructors resolving `DllName + platform suffix`, and one
portable include in the pinned MACLibDll.cpp override (SHA-256
re-pinned; Windows CI revalidated the override and the MSVC rebuild).

## 3. In scope

| Item | Notes |
| --- | --- |
| eng/build-native-codecs.sh | Pinned-source builds + hash manifest |
| NativeCodecLoader + resolver | Hash-validate, register, load, bind |
| Codec registration | libFLAC/WavPack/MACLib decs+encs, gated on readiness |
| Round-trip tests per codec | Initialize, write, finalize, read back, bit-exact compare |
| Packaging | native/ payload in .deb and AppImage; size gate re-check |
| Real conversion evidence | An owner album converted to .ape or .wv and database-verified |

## 4. Out of scope, on purpose

| Excluded | Where it connects later | Log entry |
| --- | --- | --- |
| External command encoders | SLICE-007 (D-047) | D-047 |
| WMA family | Windows-only surface | D-006 |
| TTA/OptimFROG/other niche natives | Follow-up as demand appears | D-042 |

## 5. Stubs and their debts

None new.

## 6. Modules touched

Fork codecs closure (portable preload seams, merged), Linux app
(loader, composition, csproj payload), eng (native build script,
packaging), tests.

## 7. Data subset

No new app-owned entities. `native-codecs.json` is a packaged build
artifact, not user data.

## 8. Acceptance criteria

| ID | Criterion | Verified by |
| --- | --- | --- |
| S5-001 | Each vendored codec round-trips bit-exact on Linux: initialize, write, finalize, read back | NativeCodecTests (three codecs) |
| S5-002 | A library failing hash validation leaves its codec unregistered with the reason logged; no bare-name fallback exists | Test + loader design |
| S5-003 | The packaged app ships the libraries + manifest and loads them from the packaged layout | Packaging + clean-install CI |
| S5-004 | A real album converts to a native-codec format and the output verifies with the same database identity | Evidence run |
| S5-005 | Size gates (D-025) still hold with the native payload | Package builds |
| S5-006 | Owner walkthrough | Owner sign-off (queued) |

## 9. Verification evidence required

- [x] Round-trip tests green (NativeCodecTests: libFLAC, WavPack, and
      Monkey's Audio each encode 44,100 samples, finalize, decode back,
      and compare every sample bit for bit; suite 35/35).
- [x] Real conversion + verification evidence run (2026-08-13): the
      repaired album (24 FLAC tracks) converted to Monkey's Audio through
      the vendored MACLibDll.so, published atomically, and the APE set
      verifies **Album verified - AccurateRip accurate confidence 29 /
      CTDB 207**, the same verdict as its FLAC source and the earlier
      ALAC and round-trip FLAC generations. Native encode and decode both
      proven in the production flow (verification decodes all 24 APE
      files). Screenshot: docs/evidence/2026-08-13-native-ape-verified.png.
- [x] Package size gate outputs (2026-08-13, with the native payload):
      .deb 17 MB download / 50 MB installed, AppImage 20 MB - all within
      the D-025 budgets (30 MB download / 60 MB installed). The three
      stripped libraries plus manifest cost ~1 MB.
- [ ] EDD section 17 per-change checklist per change.

## 10. Agent guardrails for this build

Boundary per section 6; D-028 as explicitly relaxed by D-042 for these
three libraries only; mode separation; conflicts stop and surface.

## 11. Slice definition of done

All acceptance criteria evidenced; documents updated; owner walkthrough
approval.

## 12. What this unlocks

- SLICE-006 settings persistence (D-043).
- SLICE-007 external command encoders (D-047).
- The rip slice's encode side is now format-complete.

---

*Approved for build by: Daniel Boyd, 2026-08-13 (D-042 selection with
dependency approval).*
