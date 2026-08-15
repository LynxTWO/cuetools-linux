# macOS roadmap

Groundwork notes for an eventual macOS release, recorded 2026-08-14 so the
port does not re-derive them. No macOS code exists yet, and that is
deliberate: platform code that cannot be run on real hardware is dark code.
The owner's direction is drift prevention now, port later, and no release
without a real Mac and real optical drives to produce evidence on.

## What is in place now

- **Fail closed everywhere macOS would have misbehaved quietly** (fork PR
  #35, verified by review and by the existing Windows and Linux suites).
  macOS reports `PlatformID.Unix`, so before this change the transport,
  lease, identity, tray, and enumeration paths would all have walked the
  Linux path. Now: device open, drive leasing, physical identity, and tray
  control throw `PlatformNotSupportedException` on non-Windows non-Linux
  platforms; drive enumeration returns no drives (honest emptiness, the app
  stays usable for verify and convert).
- **A drift-prevention CI lane** (`macos` job in `.github/workflows/ci.yml`)
  builds the app and runs the managed-only test subset on Apple silicon via
  `eng/build.sh --managed-only`. It is advisory: it exists so the eventual
  port starts from "compiles and the managed suite passes", not archaeology.
  It is not a release lane and proves nothing about optical hardware.
- **Filename discipline already fits macOS.** Cleansing uses the
  Windows-strict invalid set on every platform (fork PR #34), which covers
  `:` - the one character macOS itself treats specially. APFS defaults to
  case-insensitive, matching the cross-head collision rules (inferred; test
  on real APFS at port time).

## Known unknowns (probe on real hardware, in this order)

- **Optical transport.** macOS has no SG_IO. The candidate is IOKit
  (`SCSITaskUserClient` / `IOSCSITaskDeviceInterface`), which historically
  requires exclusive access to a drive the OS has not claimed. Unknown on
  modern macOS: entitlement and codesigning requirements, Apple silicon
  behavior, whether USB MMC drives expose pass-through cleanly, and what
  the OS-claim dance looks like while the Finder can see the disc. This is
  a full transport increment on the scale of `Bwg.Scsi/LinuxSg.cs`, plus
  enumeration, a physical-identity scheme, and tray control.
- **Calibration and cache defeat** on the macOS driver stack: unknown until
  the same probe matrix that characterized the three Linux drives runs
  there. The matrix drives are USB and can move to a Mac.
- **Cross-process lease mechanism.** `FileStream.Lock` is unsupported on
  macOS (the reason the Linux guard is `IsLinux`, not `!IsWindows`). The
  port needs `flock`/`O_EXLOCK` via P/Invoke or a protocol change; unknown
  which, decide with a two-process test like the one that caught the Linux
  fail-open.
- **Keep-awake**: IOKit power assertions (`IOPMAssertionCreateWithName`)
  are the inferred equivalent of `SetThreadExecutionState`.

## Build and packaging (all inferred, none exercised)

- **Codec natives become `.dylib`.** `eng/build-native-codecs.sh` and the
  hash manifest are `.so`-specific; the loader's known-names table needs a
  per-platform suffix. Same for the curated CLI encoders (Mach-O instead of
  ELF), with an extra wrinkle: spawned executables are subject to Gatekeeper
  quarantine, so notarization matters for them too, not just the app.
- **Packaging**: `.app` bundle plus `.dmg`; the `.deb`/AppImage scripts do
  not carry over.
- **Signing**: Developer ID certificate, hardened runtime, `notarytool`.
  NativeAOT should not need the JIT entitlement (inferred). Distributing
  unsigned means Gatekeeper friction for every user; whether to pay for a
  Developer ID is an owner decision at release time.
- **AOT**: `osx-arm64` NativeAOT publish is supported by .NET but untested
  here.

## Filesystem semantics to test against the output proofs

APFS normalizes filenames (an NFD variant), unlike ext4 which stores bytes.
Track titles with accents may come back in a different byte form than they
were written with. Publication, collision rejection, repair discovery, and
`repair.verify`/`rip.verify` path handling must round-trip under that
normalization - unknown until exact-byte tests run on real APFS. Both
case-insensitive (default) and case-sensitive APFS volumes exist; test the
default first.

## Evidence policy

Same discipline as Linux: per-drive probe matrix, live calibration
receipts, scrubbed logs (no album or artist names, no sector payloads), and
no release claim without hardware evidence. The CI lane above keeps the
code honest in the meantime; it does not substitute for any of this.
