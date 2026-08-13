# Codecs (what this build can read and write)

## The set

- **WAV** - built into the engine; uncompressed PCM.
- **FLAC** - two implementations: the managed Flake encoder (CUETools'
  own, the default) and the native libFLAC, both offered in the codec
  picker with their identity spelled out.
- **Apple Lossless (ALAC, .m4a)** - managed, compiled in.
- **WavPack (.wv)** - native, vendored.
- **Monkey's Audio (APE, .ape)** - native, vendored.

## Where the native libraries come from (and why not your distro's)

The native codecs are built from the same pinned, patched sources the
Windows release uses (the project's vendor staging), shipped inside the
app package beside a manifest of their SHA-256 hashes. At startup the
app re-hashes each library against that manifest before loading it, and
the codec wrappers load only that exact validated path - never a
library found on your system by name. A library that fails validation
leaves its codec out of the picker with the reason in the diagnostic
log; nothing is offered that cannot actually run.

Distro packages would auto-update underneath the app; a pinned,
hash-checked library is the same discipline the rest of this project
applies to everything it ships.

## Proof the natives are real

Each vendored codec is covered by a round-trip test on Linux: encode a
second of deterministic audio, finalize the file, decode it back, and
compare every sample bit for bit. The evidence run converts a real
album to Monkey's Audio and verifies the converted set against
AccurateRip and CTDB - the same database-certified round trip the
Convert page's evidence established for ALAC.
