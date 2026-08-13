# Enrich (metadata and artwork)

## The contract, in one paragraph

An album with missing or thin metadata can look itself up: the databases
propose the release's artist, album, year, genre, per-track titles, and -
when the album has no artwork - its front cover. Every proposed change is
shown as a before/after diff first, and one approval per album applies
it. Declining writes nothing; approving writes tags (and the cover) into
the audio files, never touching the audio bytes. An album that already
matches proposes nothing - the diff is honest, not busywork.

## Artwork

Covers are fetched only from the release's recorded cover-art hosts over
HTTPS, resized to the same size cap the engine uses everywhere (1500
pixels on the long edge), embedded in every track's tags, AND written as
folder.jpg beside the album - unless a folder.jpg already exists, which
is never overwritten.

## Offline

Enriching while offline queues the lookup instead of failing: an
"Enrichment pending" card appears in the rail on your next online
launch, and reviewing it generates the proposal fresh from the databases
at that moment - the queue remembers what needs enriching, never a stale
answer.

## Command line

- `cuetools-linux <album> --enrich` - look up, and apply the diff
  without the dialog (the flag is your consent, and it is logged).
