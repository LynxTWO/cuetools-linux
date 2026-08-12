# Verify

## Getting an album in

Four ways, all equivalent:

- Drop an album folder, CUE sheet, playlist, or a supported lossless file
  anywhere on the Verify page.
- The File... button (multi-select; rip sets or individual audio files).
- The Folder... button.
- Launch arguments: `cuetools-linux /path/to/album` loads it;
  add `--verify` to start the run immediately (handy from a file manager).

CUETools finds CUE sheets itself, keeps multi-disc boundaries explicit,
and verifies each disc in order. If the chosen sources overlap ambiguously
it stops and asks instead of guessing.

Supported without extra codecs: WAV, FLAC, ALAC (m4a). Sources needing
native codecs (WavPack, APE, TTA) show an honest "codec unavailable in
this build" row until the codec runtime ships.

## Reading the verdict

The album card gives one headline (for example "Album verified", "1 disc
could not be verified", "Verification complete - review the evidence")
plus disc, track, and duration counts. Each disc card carries:

- **Outcome chip**: DATABASE VERIFIED (green), NOT DATABASE-CONFIRMED
  (neutral), REPAIRABLE (amber), REPAIRED + VERIFIED (green), FAILED
  (red), WORKING, PENDING.
- **ACCURATERIP panel**: "accurate | confidence N" means N independent
  rips worldwide match yours byte-for-byte. "no match | X/Y" means the
  disc is known (Y submissions) but your rip differs. "not in database"
  means the pressing has no submissions. Subtlety worth documenting: the
  per-track columns show zero-offset confidences, so a rip made without
  drive offset correction can read "0/130" per track while the album
  verdict says "accurate | confidence 4" - the engine found the match at
  the pressing's offset and says so honestly.
- **CUETOOLS DB panel**: "verified | confidence N", "damage found |
  parity available" (repair is possible), "no exact match | N entries",
  or "not found".
- **Track evidence table**: per-track CRC32 plus AccurateRip and CTDB
  confidences. Green rows are database-confirmed tracks.
- **TOC id** (bottom right of the card): the disc's CDTOC identity that
  keys every database lookup.

## Reports

Every verify writes a dated AccurateRip report (`<cuename>.accurip`) next
to the album. It contains the same verdict vocabulary plus per-track
CRCs, and it records exactly what the databases answered that day. Reports
are evidence: the app never rewrites one (see offline-and-backfill.md for
the preserved-history rule).

Screenshots: 2026-08-11-verify-fixture-dark.png (unknown album, honest
"not in database"), 2026-08-12-verify-real-disc-dark.png (real pressing,
accurate | confidence 4), 2026-08-11-theme-*.png (both themes).
