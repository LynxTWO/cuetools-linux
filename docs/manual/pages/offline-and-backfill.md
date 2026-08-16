# Verify offline, and let backfill catch up

CUETools verifies an album with no network at all. Your files are still
decoded, every checksum is still computed, and the report is still
written next to the album. What is missing is the comparison with the two
databases (AccurateRip and CTDB), so CUETools records which albums still
need that answer and asks for it on a later launch. That catching up is
called [backfill](glossary.md#backfill). An offline verification reads
your audio files and does not change them. A backfill later writes a new
report over the album's earlier one, after trying to keep a copy of it.

## Before you start

| Item | What to know |
| --- | --- |
| What still works | Loading an album, decoding it, the per-track checksums, the on-screen track evidence, and the `.accurip` report file. |
| What is missing | The AccurateRip and CTDB comparisons. Both panels read `lookup failed`, and no disc can be confirmed. |
| What gets queued | One entry per disc, in a small file under `~/.local/state/cuetools-linux/journal/`. |
| Original files | Read only, both offline and during a backfill. Neither one changes your audio or its tags. |
| Report file | A backfill writes a fresh report over the album's old one. It tries to copy the old one to a `.pre-backfill` name first, which usually works but is not guaranteed. |
| When it catches up | On a later launch, when a database answers. Backfill does not run while the app is already open. |
| Not queued | [Repair](repair.md) needs CTDB while it runs, and a disc is only marked repairable by a CTDB lookup that completed, so there is no offline repair to queue. Ripping a disc offline queues nothing either. |

## Verify with no connection

1. Open the **Verify & Repair** page and load the album the usual way:
   drag it onto the page, or press **File...** or **Folder...**. See
   [verify an album](verify.md).

2. Press **Verify album**. Nothing asks you to confirm that you are
   offline, and nothing is skipped: the run reads and checksums every
   file either way.

3. An offline disc adds this to the end of its status text:

   ```text
   Offline: database verification queued for automatic backfill.
   ```

   Do not rely on reading it. The next status message replaces it
   immediately, on both the disc card and the bottom status line, so it
   can pass by before you look. The queue does not depend on your seeing
   it.

4. Read the disc card. Both database panels read `lookup failed`, and
   the chip in the card's top-right corner reads
   `NOT DATABASE-CONFIRMED`.

Nothing else is required of you. The queued entry survives closing the
app, and you do not have to load the album again to have it caught up.

## Read an offline result

When every disc completed, the album card at the top reads
`Verification complete - review the evidence`, with a tally underneath
counting each disc as `not database-confirmed`. That is the same headline
you get for a disc no database happens to know, so the panels on the disc
card are what tell the two apart.

| Where | What it says offline | What it means |
| --- | --- | --- |
| ACCURATERIP panel | `lookup failed` | The lookup did not complete. It is not the same as `not in database`, which is AccurateRip answering that it has nothing for this disc. |
| CUETOOLS DB panel | `lookup failed` | The same, for CTDB. Its answer for a disc it genuinely does not hold is `not found`. |
| Chip | `NOT DATABASE-CONFIRMED` | The disc was read and checksummed, and no database confirmed it. |
| Message line under the track table | `AR: database access error: ..., CTDB: database access error: ...` | The reason each lookup failed, in the engine's words. The text after `database access error:` comes from the network stack, so it varies. |
| **AR conf** and **CTDB conf** columns | `-` on every track | No database returned a count for any track. |

The message line is worth reading. A run made with no network at all
names both databases, as in this one from a verification with the network
switched off:

```text
AR: database access error: Resource temporarily unavailable
(www.accuraterip.com:443), CTDB: database access error: Resource
temporarily unavailable (db.cuetools.net:80)
```

Your track [CRC32](glossary.md#crc32) values are still in the table, and
still in the report file, because they are computed from your audio and
owe nothing to either database.

## What gets queued

Each disc that finished verifying writes one entry into the backfill
journal, a folder of small JSON files at
`~/.local/state/cuetools-linux/journal/` (or under `$XDG_STATE_HOME` if
you set that variable). One file is one queued disc, and it records the
path CUETools verified, that disc's
[TOC id](glossary.md#toc-id-table-of-contents-id), when the entry was
made, its state, how many times a replay has been attempted, and, once
it is done, the report that resolved it.

A few things are worth knowing about the queue:

- A disc that failed to verify is not queued. Only a disc with a
  completed local result has something left to finish.
- Verifications started from the [Queue page](queue.md) are queued the
  same way. An offline queue item ends as `No match`, and the text beside
  it carries the same database access errors.
- Verifying the same album offline twice queues it twice, and a replay
  works through both entries.
- Nothing in the app shows you the queue. If you want to see it, list
  that folder.

## How backfill catches up

Every time you launch CUETools, it checks the queue in the background
once the window is open. The app stays usable while this happens, and
there is no progress bar or notification for it.

If a database answers, each queued entry is worked through in the order
it was made. The album is verified again from scratch, which means its
files are decoded and checksummed a second time, so a long queue takes
about as long as running those verifications by hand.

| What the replay finds | What happens to the entry |
| --- | --- |
| The files are where they were, and the verification completes | The entry is marked resolved and records a report path: whichever `.accurip` in that folder was written most recently. That is the fresh report in a single-disc folder, but not always in a folder holding several discs. |
| The files are still there, but the verification fails | The entry stays queued, with the attempt counted. The next launch tries again. There is no limit on attempts. |
| The path no longer exists | The entry is marked unresolvable, with the reason "The journaled source no longer exists at its recorded path." It is kept as a record and is not tried again. |
| No database answers | Nothing is replayed at all, and every entry stays queued for a later launch. |

The Verify & Repair page does not change when a backfill runs, even if
the album is loaded in front of you. The result of a backfill is the
files it wrote next to the album.

## The report a backfill replaces

A backfill writes a complete new report, the same one a verification you
ran yourself would write, with the current date on its first line and the
databases' answers in its body.

Before re-verifying, CUETools tries to copy the album's existing report
to a name ending in `.pre-backfill`, so the offline-era answers stay on
disk beside the new ones. The copy is named after the report, with the
time it was taken in UTC, like this:

```text
album.accurip
album.accurip.20260812-234107.pre-backfill
```

Treat that copy as a convenience rather than a guarantee. It is skipped,
with only a line in the diagnostic log, when the copy cannot be made, for
example if the folder is not writable or a file of that name already
exists. It also copies whichever `.accurip` file in that folder was
written most recently, which in a folder holding reports for several
discs is not always the one about to be replaced. If a particular report
matters to you, copy it somewhere yourself.

An ordinary re-verification, one you start yourself, does not make a
`.pre-backfill` copy. It writes the new report over the old one.

## Check whether a backfill ran

There is no on-screen answer to this, so look at what a backfill leaves
behind.

- **The report's first line.** Every report starts with the date it was
  written and the CUETools engine version, so a report dated later than
  your offline run is the backfilled one.
- **A `.pre-backfill` file** next to it, holding the offline-era report.
- **The diagnostic log**, in `~/.config/CUETools2026/logs/`. The replay
  writes its own lines under the category `backfill`, one per entry
  (`backfill: <id> resolved`, `backfill: <id> unresolvable (source
  missing)`, `backfill: <id> retry later (verify failed)`) and one
  summary line, for example `replay done: 1 resolved, 0 unresolvable, 0
  still pending`. When nothing could run, the line reads `backfill: 1
  entries pending, still offline`. The log records structure only, never
  album or artist names.
- **The journal entry**, which now reads `"state": "Resolved"` and names
  the fresh report in its `resolutionEvidencePath` field.

## If something goes wrong

### One panel reads `lookup failed` and the other has a real answer

Only that one service failed. CUETools treats a run as offline only when
neither database answers a connection attempt, so a single service being
down does not queue anything, and nothing will catch this album up on its
own. Verify the album again yourself when the service is back.

### You relaunched and nothing seems to have happened

The replay has no screen of its own, so an empty queue and a successful
replay look identical from the window. Check the report date next to the
album, or the `backfill` lines in the diagnostic log.

If both databases were still unreachable at launch, nothing was
replayed, and the log says so: `backfill: 1 entries pending, still
offline`. Those entries stay queued for the next launch.

### The report was replaced, and there is no `.pre-backfill` copy

The copy is best-effort. Two things stop it being made at all: the
folder is not writable, or a file of that exact name already exists. A
third makes it the wrong copy without skipping it, because the snapshot
is taken from whichever `.accurip` in the folder was written most
recently, which in a multi-disc folder can belong to another disc. Copy
a report you want to keep yourself, before the next launch.

### A backfilled report still shows a database access error

An entry is marked resolved when its verification completes, whatever
the databases answered. The connection check runs once, before the queue
is worked through, so a network that drops in the middle of a replay
still leaves resolved entries behind. Verify that album again yourself.

### An album you moved never gets its answers

Moving, renaming, or deleting the files makes the entry unresolvable at
the next launch, and unresolvable entries are not retried. Load the album
at its new location and verify it again.

## Metadata lookups queue differently

**Enrich metadata...** on a disc card also needs the databases. Pressing
it with no connection shows a window titled "Enrich metadata" reading:

```text
The lookup failed: The databases are unreachable. The lookup was
journaled and will be offered again on an online launch.
```

That request goes into the same journal, in its own lane, and it is not
replayed silently. On a later launch a card reads
**Enrichment pending (1)** in the left rail, with "Offline lookups ready
to review" beneath it. Pressing it looks each album up at that moment
and shows you the usual before-and-after diff to approve or decline, so
what you are offered is the databases' current answer and never a stored
one. Pressing it while still offline stops at the first album it can
still find, and that album and everything after it stay pending with no
message. It is not completely inert: any album whose files have been
moved or deleted is retired from the card before it stops.

Asking to enrich the same album twice while offline queues it once.

## Command line

```console
cuetools-linux --verify /path/to/album
```

This runs a verification the same way the window does, so it queues a
backfill entry when there is no connection, and writes the report either
way. `/path/to/album` is an album folder, CUE sheet, playlist, or
supported lossless audio file.

Verifying an album yourself does not clear an entry already queued for
it. The next launch replays that entry too, which verifies the album once
more and, this time, copies the report from your own run to a
`.pre-backfill` name.

## How it works

"Offline" is a direct question, not an assumption from a failed lookup.
Before each disc is verified, CUETools opens a TCP connection to
`db.cuetools.net` on port 80 and to `www.accuraterip.com` on port 443,
allowing each attempt three seconds. If either one answers, the run is
online and nothing is queued, even if a lookup then fails. Only when
neither answers does the disc's result get a journal entry. That is why
one service having a bad day never queues a backfill: the other database
answered, and its answer is real. A network that blocks both of those
addresses makes every run look offline to CUETools, whatever the rest of
your connection is doing.

The panels report the two cases separately. A lookup that failed reads
`lookup failed`; a database that answered and had nothing for your disc
reads `not in database` for AccurateRip and `not found` for CTDB. A
database that answered before something else went wrong still shows its
answer, because the failure wording is only reached when there is no real
result to show.

Journal entries are written one file at a time, to a temporary name that
is then renamed into place, so a crash mid-write cannot leave a
half-written entry. An entry is never deleted by the app: it moves from
queued to resolved or unresolvable and stays there. An entry written by a
newer version of CUETools than the one reading it is left alone rather
than discarded.

The replay verifies through the engine directly, one step below the layer
that queues offline runs. A replay that races a network drop therefore
cannot queue a fresh copy of the entry it is in the middle of resolving.

A backfill sends the same lookups a verification sends: AccurateRip is
asked about the disc's identifiers, and CTDB is asked what it holds for
the disc's table of contents. Nothing in this build sends your audio,
your checksums, or your reports to either database.

## Related topics

- [How to verify an album, and how to read the verdicts](verify.md)
- [How Repair rebuilds a damaged album from CTDB parity](repair.md)
- [Batch verifying with the Queue](queue.md)
- [Where CUETools keeps its files, and what leaves your machine](install.md)
- [Terms used in this manual](glossary.md)
