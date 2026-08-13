# CUETools Linux Slice Brief: SLICE-007 Additional Encoders

Version: 0.1. Date: 2026-08-13. Status: Approved for build (owner selected
"in scope soon" 2026-08-13, D-047); increment A shipped, increment B
scoped.
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md,
SLICE-005-codec-runtime.md.

## 1. What the slice adds, and why it is next

- **Capability:** the lossy output formats the Windows head offers:
  MP3 (increment A, shipped), and the curated command-line encoders -
  Opus, Ogg Vorbis, Musepack (increment B).
- **Why now:** owner selected it after settings persistence (D-047).
  Archival lossless is complete; portable lossy output is the remaining
  everyday conversion need.

## 2. Increment A - MP3 (shipped)

MP3 is not an external encoder on the Windows head: it is the native
libmp3lame wrapper. So it joins the D-042 vendored-native model exactly
like FLAC/WavPack/Monkey's Audio:

- The official LAME 3.100 source archive is pinned in the fork
  (`ThirdParty/lame`, SHA-256 in the native dependency manifest, verified
  against the canonical published hash). License: LGPL-2.0-or-later;
  corresponding source ships by repo link (D-052).
- `eng/build-native-codecs.sh` builds `libmp3lame.so` from that archive;
  the loader validates and binds it like the other three; the CBR and
  VBR encoder faces register only when the library is ready.
- Encoder-only: the lame wrapper has no decoder, so MP3 appears in the
  output lists but not as a verifiable input.

Fork PR #21 (portable preload + pinned archive; Windows CI revalidated
the manifest and MSVC build).

## 3. Increment B - curated command-line encoders (scoped, next)

The Windows head curates opusenc, oggenc2, and mpcenc through
`eng/release/external-command-encoders.json`: pinned source archives
with hashes, recorded licenses with license-text files, executable
hashes, and runtime re-validation, with receipt-bound user imports
resolved ahead of bundled fallbacks. The Linux increment:

1. Build ELF binaries from the SAME pinned source archives (plus the
   fork's cuetools patches for opus/musepack) - the license and
   source-obligation review already recorded in that manifest carries
   over; only the build products are new.
2. A Linux section or sibling manifest records the ELF hashes; the app
   re-validates before offering the encoder (real CLI execution check,
   not just file presence - an encoder that cannot run is never a
   selectable row).
3. The user-import flow (receipt-bound, overriding bundled copies)
   ports with the same discipline.

This increment is its own work block: the curated-encoder trust flow is
the largest remaining Windows-parity surface outside rip.

## 4. Acceptance criteria

| ID | Criterion | Verified by |
| --- | --- | --- |
| S7-001 | MP3 encodes through the vendored LAME: initialize, write, finalize, valid MPEG stream | Mp3EncodesAValidStream |
| S7-002 | A real album converts to MP3 in the app and publishes atomically | Evidence run |
| S7-003 | (B) Each curated CLI encoder builds from its pinned source, validates by hash + real execution, and converts a real album | Increment B evidence |
| S7-004 | Owner walkthrough | Owner sign-off |

## 5. Verification evidence

- [x] S7-001: suite 39/39 (the test skips the ID3v2 tag by its syncsafe
      length and requires the MPEG frame sync at audio start).
- [x] S7-002 (2026-08-13): the repaired album converted to MP3 in the
      app through the vendored LAME (VBR faces registered from the
      validated library), 24 files published atomically, 91 MB for
      1:08:22 of audio. Screenshot:
      docs/evidence/2026-08-13-mp3-convert-complete.png.
- [x] S7-003 (2026-08-13): all three curated encoders built from pinned
      sources as self-contained ELF binaries under the catalog's
      cross-head identities (opusenc.exe: opus-tools 0.2 with the
      CUETools opus/libopusenc patches; mpcenc.exe: Musepack r495 with
      the CUETools patch, announcing "CUETools source build r495";
      oggenc.exe: official vorbis-tools 1.4.2 - the recorded Linux
      stand-in for the MSVC-only rarewares oggenc2 drop, using only the
      engine template's standard flags). eng/build-cli-encoders.sh
      verifies every archive against its pin before use and emits
      linux-encoders.json with hashes, versions, and licenses; the
      catalog receives that table through the packaged-host constructor
      (fork PR #22) and refuses hash-mismatched binaries (pinned by
      test). Five CliEncoderTests; suite 44/44. Evidence: the repaired
      album converted to Opus in-app, 24 files published atomically
      (docs/evidence/2026-08-13-opus-convert-complete.png).
- [x] Size gates with the encoder payload (2026-08-13): .deb 17 MB
      download / 50 MB installed, AppImage 20 MB - within D-025.
- [ ] EDD section 17 per-change checklist per change.

---

*Approved for build by: Daniel Boyd, 2026-08-13 (D-047 selection).*
