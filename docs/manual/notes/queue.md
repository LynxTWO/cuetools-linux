# Queue (batch verify and convert)

## The contract, in one paragraph

Stack album folders or .cue files, choose what to do with each batch
(Verify or Convert, with the codec picker for conversions), and run them
in one sitting. Items execute one at a time; every finished item keeps
its own verdict and detail, and nothing a finished item reported is ever
erased by a later one. A queued conversion pins its exact codec
implementation when you enqueue it - if that implementation is no longer
ready when the batch reaches the item, that item fails with the reason
and the batch continues.

## Verdict vocabulary

Verify items end as **Verified** (database-confirmed), **Repairable**
(damage that CTDB parity can recover), **No match** (no database
confirmation), or **Failed** (with the error). Convert items end as
**Done** (with the file count) or **Failed** (with the reason). The
vocabulary is the Verify page's own; the queue never invents softer
words.

## Command line

- `cuetools-linux --queue <path> <path> ...` - enqueue the paths and
  land on the Queue page.
- `--queue-run` - additionally start the batch.

## The real batch (2026-08-13 evidence run)

Four albums queued in one batch: the damaged rip from the repair
walkthrough and its three descendants (the repaired FLAC copy, the
converted ALAC copy, and the ALAC-to-FLAC round trip). The queue ended
"Batch complete: 4/4 processed" with the damaged rip **Repairable**
(rip not accurate 0/82, CTDB differs in 7,150 samples) and all three
derived albums **Verified** (rip accurate 29/82, CTDB confidence 207).
Three codecs, three conversion generations, one identical verdict - the
databases certify the whole conversion chain bit-exact. Screenshots:
2026-08-13-queue-batch-running.png,
2026-08-13-queue-batch-complete.png.
