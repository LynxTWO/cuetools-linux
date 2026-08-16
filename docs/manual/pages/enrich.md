# Enrich an album

Enrich looks an album up in the
[CUETools Database (CTDB)](glossary.md#ctdb-cuetools-database) and offers
to fill in what it is missing: album artist, album title, year, genre, the
per-track titles, and a front cover. Every change it proposes is shown
first, as a before-and-after list, and nothing is written until you
approve that list. Approving rewrites the [tags](glossary.md#tags) inside
your audio files, and can add a `folder.jpg` to the album folder. The
audio in those files is not re-encoded and not altered, and the album's
CUE sheet is left exactly as it is.

## Before you start

| Item | What to know |
| --- | --- |
| Input | One disc on the **Verify & Repair** page. **Enrich metadata...** appears on a disc card once that disc has a result, and each disc of a multi-disc set is enriched on its own. |
| Result | Album and track tags written into that disc's audio files, plus a `folder.jpg` in the album folder when a cover is applied. |
| Original files | Only the tags change. The audio is not re-encoded, and the CUE sheet is not rewritten. |
| Network | Required. The lookup asks CTDB. With no connection the request is queued for a later launch instead; see [Enrich with no connection](#enrich-with-no-connection). |
| Consent | Nothing is written until you press **Apply these changes**. **Cancel** writes nothing. |
| Choice of release | None. CUETools takes the first release the lookup returns for this disc's track layout, and there is no chooser. Read the list before approving it. |

Enrich only fills in and corrects. A field the release has nothing for is
never proposed, so it cannot blank out something you already have.

On an album that is one large audio file with a CUE sheet beside it, only
the album-level fields are proposed. Track titles in that layout live in
the CUE text, and Enrich does not rewrite CUE files.

## Preview and apply the changes

1. Verify the album first. Load it on the **Verify & Repair** page and
   press **Verify album**; see [verify an album](verify.md).

2. Find the disc card you want, and press **Enrich metadata...**. It sits
   under the outcome chip in the card's top-right corner, below
   **Repair this disc** when that button is there too.

   The lookup runs in the background. Nothing on the page changes while
   it does, and the next window opens when the answer arrives.

3. Read the **Proposed changes** window. Under the heading, a line
   beginning "Source:" names where the release came from (MusicBrainz,
   for example), with its web address when the release has one. Under
   that: "Nothing is written until you approve. Approving applies every
   change below to this album's audio file tags; the files' audio bytes
   are untouched."

4. Read the list. Each row names one field on its own line, then shows
   what the album says now, an arrow, and what the release says. The
   album-level rows are named `Artist`, `Album`, `Year`, and `Genre`,
   and track rows are numbered, as in `Track 01 Title`. A cover is one
   row of its own, and it is the row whose wording never changes:
   `(none) -> cover image from the release`.

5. Approve or decline the whole list:

   - **Apply these changes** writes every row in it.
   - **Cancel** writes nothing. Closing the window has the same effect.

   There is no way to approve part of the list. If one row is wrong, the
   answer is **Cancel**.

6. A window titled "Enrich metadata" reports what was written: "Applied
   N change(s) across M file(s)." N is the number of rows you approved,
   and M is the number of audio files whose tags were rewritten.

The disc card itself does not change. It keeps showing the artist and
album the verification found, so an album that read "Unknown Artist -
Unknown Album" still reads that way until you verify it again.

## What gets written where

| List row | What Enrich writes |
| --- | --- |
| `Artist` | The album artist tag and the track artist tag, in every one of that disc's audio files. |
| `Album` | The album tag, in every file. |
| `Year` | The year tag, in every file, when the release's year is a plain number. |
| `Genre` | The genre tag, in every file. |
| `Track 07 Title` | The title tag of the seventh track's file. That file's track number is written at the same time. |
| `Front cover` | A front-cover picture embedded in every file, replacing any picture that file already carried, and a `folder.jpg` in the album folder. An existing `folder.jpg` is never overwritten. |

Nothing else in the files is touched. Tags Enrich has no row for, custom
ones included, are left where they are.

The CUE sheet keeps its own copy of the artist, album, and track titles,
and Enrich does not update it. A player that reads the CUE sheet rather
than the files will still show the old names.

## Read the result

All of these arrive in a window titled "Enrich metadata". The proposal
carries its own two buttons; the rest hold their message and nothing
else, so you close them with the window's close button.

| Message | What it means | What to do next |
| --- | --- | --- |
| The **Proposed changes** window, with rows in it | The lookup found a release, and these are the differences from your album. | Approve or cancel; see [Preview and apply the changes](#preview-and-apply-the-changes). |
| `This album already matches the database release - nothing to change.` | A release was found, and every field it carries already agrees with your tags. | Nothing; the album is already described the way the database describes it. |
| `No database release was found for this album.` | Nothing usable came back for this disc's track layout: no release carrying both an artist and an album title. A lookup that errored after the connection check passed reads the same way. | Try again later in case it was the lookup rather than the disc; see [Nothing was found, and you expected a release](#nothing-was-found-and-you-expected-a-release). |
| `Applied N change(s) across M file(s).` | The approved rows were written. | Verify the album again if you want the disc card to catch up. It catches up only where the CUE sheet leaves a name blank; where the CUE text carries its own artist or album, that is what the card keeps showing. |
| `The lookup failed: The databases are unreachable. The lookup was journaled and will be offered again on an online launch.` | CUETools could not reach either database, so it queued the request instead. | Nothing now. See [Enrich with no connection](#enrich-with-no-connection). |
| `The lookup failed:` followed by an error | CUETools could not open the album: an unsupported or unreadable source file, or audio the CUE sheet names that is not there. The text after the colon is the underlying error. | Fix what the message names, then press **Enrich metadata...** again. |
| `Applying failed:` followed by an error | Writing stopped part way. The text after the colon is the underlying error, for example a file that is not writable. | Fix what the message names, then run Enrich again. Fields already written are not proposed a second time. |

A failure while writing leaves the files it had already finished with
their new tags. There is no undo, and nothing is rolled back.

## Enriching the same album twice

Enrich compares the release against the tags in your audio files rather
than against the CUE sheet, so an album you have already enriched has
nothing left to propose. The second run says
`This album already matches the database release - nothing to change.`,
and it says that even though the CUE text still carries the old names.

Anything the first run did not write does come back. A row you declined
is offered again, which is the point of declining. So is a year the
release states in some form other than a plain number: that row is
proposed but never written, so it returns every time. Set that one
yourself if it bothers you.

## Enrich with no connection

Before looking anything up, CUETools checks whether it can reach the
databases (AccurateRip and CTDB). When neither answers, nothing is asked
of them. The album is recorded instead, in the same journal offline
verifications use and in a lane of its own, and the window says so:

```text
The lookup failed: The databases are unreachable. The lookup was
journaled and will be offered again on an online launch.
```

Asking twice for the same album records it once.

On a later launch, a card headed **Enrichment pending**, with the number
of waiting albums in brackets, appears at the bottom of the rail on the
left, under SESSION.

![The CUETools Linux window with nothing loaded, in the dark theme. The header reads CUETOOLS LINUX with a Light theme button at the far right. The left rail lists, under WORK, "Verify & Repair / AccurateRip + CTDB" highlighted with a teal border, and "Convert / Re-encode existing rips"; under SESSION it lists "Queue / Batch verify or convert" and "Enrichment pending (1) / Offline lookups ready to rev...". The page holds an empty Source box with File... and Folder... buttons, a greyed-out Verify album button, and a large drop target reading "Drop an album folder here".](2026-08-13-enrich-pending-rail.png)

*One album waiting to be reviewed. The card's second line, cut off at the
rail's width, reads "Offline lookups ready to review". This screenshot is
from an older build, whose rail had no Rip card at the top.*

Press the card to work through the waiting albums. Each one is looked up
again at that moment, so what you are offered is the databases' current
answer rather than a stored one, and each proposal goes through the same
approve-or-cancel window. Albums that need no change, or that have no
release, are cleared without showing you anything, so the count can drop
with no window appearing. When the last one is dealt with, the card goes
away.

Three things are worth knowing. An album whose journaled path you have
moved or deleted is retired from the card rather than offered. An album
whose path is still there but that CUETools cannot open, audio files
moved out from under the CUE sheet for instance, is skipped in silence
and stays on the card. And pressing the card while still offline stops at
the first album it can still find, saying nothing at all; that album and
the ones after it stay waiting.

See [offline behavior and backfill](offline-and-backfill.md) for the
verification side of the same journal.

## If something goes wrong

### The disc card has no Enrich metadata... button

The button appears once that disc has a result. Press **Verify album**
and let the disc finish.

### The album still reads "Unknown Artist - Unknown Album" on the card

The card reports what the verification found, and applying tags does not
re-run it. The files on disk carry the new tags. Verify the album again
to see the card catch up.

### No front cover was offered

Enrich proposes a cover only for an album that has none. It counts as
having one when a picture is embedded in the album's first audio file, or
when the folder holds `folder.jpg`, `Folder.jpg`, or `cover.jpg`. Other
loose image files in the folder can count as well.

### The cover was applied, but folder.jpg is the old image

An existing `folder.jpg` is never overwritten, and that is the case
whether the file came from CUETools or from you. Rename or move the old
one, then enrich again.

### The proposal describes a different album

The databases find a disc by its track layout, and two different
[pressings](glossary.md#pressing) can share one. Press **Cancel**; there
is no way to ask for a different release.

### Nothing was found, and you expected a release

`No database release was found for this album.` covers two situations
that look identical from the window: CTDB has no metadata for this track
layout and the freedb fallback found none either, or the lookup itself
went wrong after the connection check had already passed. A rare or
regional pressing genuinely being unknown is the common case. Trying
again later is what separates the two.

## Command line

```console
cuetools-linux --enrich /path/to/album.cue
```

The flag is your consent, given up front: the album is looked up and the
proposal is applied without the approval window. `--repair` works the
same way; see [repair](repair.md).

`/path/to/album.cue` is the album's CUE sheet; a playlist or a single
supported audio file works the same way. Only the first path that exists
is enriched, even if you pass several.

The window still opens, on the Verify & Repair page, with nothing loaded,
and nothing about the enrichment appears on screen. What happened is
recorded in the diagnostic log in `~/.config/CUETools2026/logs/`, under
the category `enrich`. One of these lines follows the consent line:

```text
--enrich: diff auto-approved by command-line consent
--enrich applied 27 change(s) across 24 file(s) from <provider>
--enrich: album already matches the database
--enrich: no database release found
--enrich failed: EnrichmentOfflineException
```

`<provider>` is the release's source, the same one the approval window
names.

A failure logs only the error's type name, as in the last line, so the
log tells you that the enrichment did not happen rather than why.

Run with no connection, `--enrich` queues the album exactly as the button
does. It logs `offline: enrichment lookup journaled`, fails with the line
above, and the album turns up on the **Enrichment pending** card at the
next launch.

The log records structure, not your music. Album and artist names are
registered for scrubbing when the album is opened, so they do not reach
the file.

## How it works

The lookup is CTDB's metadata search, asked at `db.cuetools.net` for the
disc's track layout. If CTDB returns no metadata for the layout, the
engine also asks freedb. Both requests are described in
[what leaves your machine](install.md#what-leaves-your-machine). Nothing
is submitted: this build has no submission path, so enriching an album
adds nothing to any database.

The lookup answers with a list of candidate releases, and CUETools also
puts your own album's current values into that list, once as the CUE
sheet has them and once as the tags have them. Those two are skipped, and
the first remaining candidate that carries both an artist and an album
title becomes the proposal. That is why the window can say a release was
found while proposing nothing to change: the release agrees with you.

The comparison is made against the album's effective metadata, meaning
the CUE sheet's values with the audio files' tags winning wherever a tag
is set. The tags are what an applied change writes, so the second run
over an enriched album finds nothing to do. In the 2026-08-13 evidence
run for this feature, an album that had been verifying for two days as
"Unknown Artist - Unknown Album" applied 27 changes across 24 files. A
later run over the same album proposed one change, the front cover it
still lacked.

Cover images are fetched only over HTTPS on the standard port, and only
from `coverartarchive.org`, `db.cuetools.net`, or `archive.org` and its
subdomains (the Cover Art Archive redirects to a numbered
`*.archive.org` server). An address
the release gives as plain HTTP is turned into HTTPS before it is
checked, and an address anywhere else is not fetched at all, so no cover
row appears for it. The image is capped at 1500 pixels on its longest
side and re-encoded as JPEG, unless it arrives as a JPEG already within
the cap, in which case its bytes are kept as they are. That album's cover
came down larger than the cap and was saved as a 1500x1483 JPEG. The same
bytes are embedded in every track file and written once as `folder.jpg`.

Nothing is downloaded or opened while the list is on screen. The cover
image is fetched, and the album's files are opened for writing, only
after you approve.

## Related topics

- [How to verify an album, and how to read the verdicts](verify.md)
- [What happens when you verify offline, and how backfill catches up](offline-and-backfill.md)
- [Converting an album, which names folders from these same tags](convert.md)
- [Where CUETools keeps its files, and what leaves your machine](install.md)
- [Terms used in this manual](glossary.md)
