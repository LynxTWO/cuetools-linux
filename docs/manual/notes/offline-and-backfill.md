# Offline behavior and verification backfill

## What happens offline

Local work never needs the network. A verify run without connectivity
still decodes everything, computes every checksum, and writes the dated
report - the report itself records the truth, for example:

    [AccurateRip ID: ...] database access error: Resource temporarily
    unavailable (www.accuraterip.com:443).

The status line adds: "Offline: database verification queued for
automatic backfill." Nothing blocks, nothing is lost, and nothing
pretends to be verified.

Offline detection is a direct connectivity probe of both database
endpoints; the app only treats a run as offline when BOTH are
unreachable, so a single service outage never queues spurious backfill.

## Automatic backfill

Each offline verify writes one journal entry (under
`~/.local/state/cuetools-linux/journal/`). On a later launch with the
network back, the app replays pending entries automatically in the
background:

- The album is re-verified in full; the databases answer for real this
  time, and a fresh dated report is written.
- The offline-era report is preserved byte-for-byte first, as
  `<name>.accurip.<timestamp>.pre-backfill` - history is never rewritten.
- The journal entry becomes Resolved and records the fresh report's path.
- If the album's files have moved, the entry becomes Unresolvable with a
  plain reason; it is kept, never silently dropped.

A backfilled verdict is deterministic given your files and that day's
database answer; both reports carry their dates, so the sequence of
evidence reads honestly forever.

Screenshot: 2026-08-12-s002-offline-verify.png.
