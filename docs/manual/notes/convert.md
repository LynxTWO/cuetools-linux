# Convert

## The contract, in one paragraph

Pick a source (a .cue sheet, an album folder, or a file with an embedded
cue), choose an output format, and CUETools re-encodes every track into a
new folder, keeping the album's cue fidelity. The source files are never
changed. Only formats with a working encoder in this build are offered,
and a codec that cannot run right now says why and cannot be chosen - an
extension alone is not implementation identity.

## The flow (from the 2026-08-13 evidence run)

1. Source bar: File... / Folder... pick the source; the codec button
   shows the selected implementation ("Apple Lossless (ALAC) (.m4a) -
   ..."), and "tune" opens the encoder settings for the selected format.
2. Convert runs with live progress. Lossless targets verify themselves
   while writing (the staging folder briefly holds
   `.cuetools-lossless-*` temp names - that is the encoder re-decoding
   its own output before publishing it).
3. The output is built in a hidden staging folder and published
   atomically: the final folder appears complete, with a `.cuetools-complete`
   marker, or not at all. A failed conversion leaves no partial output
   masquerading as finished.
4. The result state reports exactly what was written and where ("Wrote
   24 m4a file(s) to .../Artist - Album").

## Proof that conversion is honest

The evidence run converted the repaired album from the repair
walkthrough (24 FLAC tracks) to Apple Lossless on Linux, then verified
the converted set on the Verify page. The verdict: **Album verified,
AccurateRip accurate confidence 29, CUETools DB verified confidence
207** - the identical verdict the FLAC source earns, because a lossless
conversion is bit-exact and the databases can prove it. Screenshots:
2026-08-13-convert-alac-complete.png,
2026-08-13-convert-alac-verified.png.

## Command line

- `cuetools-linux <path> --convert` - load the source into Convert and
  start with the current defaults.
- `--convert-to <format>` - select the output format (e.g. m4a, flac,
  wav) when the build has a ready encoder for it.
- `--convert-out <dir>` - output folder; default is the page's usual
  Music/CUETools/Artist - Album layout.

## Reading the scopes

Before a source is chosen, CodecScope shows what the target codec does:
the real four-stage pipeline (signal -> predict -> residual -> pack) run
each frame by the codec family's actual predictor, with the live
bits/sample and percent-of-PCM computed from the real residual - a
better predictor genuinely earns a smaller number. With a source chosen,
ConvertScope shows the round trip: the source unpacking to PCM (the
shared currency), and the target re-packing it, each side at its real
compactness. During a conversion the middle card carries the real
decoded audio. Screenshots: 2026-08-13-codecscope-idle.png (FLAC at 5.1
bits/sample, ~32% of PCM), 2026-08-13-convertscope-live.png (ALAC ->
PCM -> FLAC mid-conversion, both sides 9.0 bits/sample - two Rice-family
codecs packing the same audio the same size, shown honestly).

## Not yet written

- Encoder settings tour (the tune dialog) with real screenshots.
