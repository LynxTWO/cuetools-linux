# Run a batch of albums

The Queue page runs a stack of albums through Verify or Convert in one
sitting, one album at a time, without you sitting at the keyboard between
them. Each item runs the same work the single-album pages run, so a
queued verify reads your audio and writes a report beside it, and a
queued conversion writes a new album folder and leaves your source files
alone.

## Before you start

| Item | What to know |
| --- | --- |
| Input | One CUE sheet (`.cue`), one `.m3u` or `.m3u8` playlist, or one audio file with a CUE sheet embedded in its tags, per item. Album folders do not work here; see [If something goes wrong](#an-item-reads-failed-and-is-a-directory). |
| Result | A verdict per item on screen. Verify items also write a `.accurip` report and a `.toc` file beside the album. Convert items write a new album folder. |
| Original files | Read only. Neither action changes your audio files or their tags. |
| Network | Verify items contact AccurateRip and the CUETools Database (CTDB). Convert items do not use the network. |
| Output folder | Queued conversions always go under `CUETools` in your Music folder, which is `~/Music/CUETools` on a standard setup. This page has no output-folder control. |

A batch cannot be stopped once it starts, and items cannot be removed one
at a time. Plan the list before you press **Run all**.

## Queue a batch

1. Open the **Queue** page, under SESSION in the sidebar.

2. Set **Action** first. The dropdown offers `Verify` and `Convert`, and
   whichever one is showing is stamped onto each item as you add it.
   Changing it afterwards does not change items already in the list.

3. For conversions, choose the output codec now as well. Press the codec
   button next to **Action**, which shows the current choice
   (`FLAC (.flac) - CUETools managed encoder` on a fresh install), pick a
   row in the **Choose output codec** window, and press
   **Use this codec**. The button is greyed out while **Action** is
   `Verify`.

   Like the [Convert page](convert.md), each item pins the exact encoder
   that was selected when you added it, so changing the codec later
   affects only the items you add after the change.

   There is no **tune** button here. A queued conversion uses the encoder
   settings currently saved for that codec, which you set with **tune**
   on the [Convert page](convert.md).

4. Press **Add file(s)...** and pick the albums' CUE sheets or playlists.
   The picker takes more than one file at a time. Its first filter,
   **Rip sets (*.cue, *.m3u, *.m3u8)**, shows all three; the other two
   are **Audio with embedded cue** and **All files**.

   This page has no drop target, so dragging files onto it does nothing.

   Each item appears as a row, and the status line at the bottom reads
   the running count: "4 item(s) queued."

5. To mix verifies and conversions in one batch, change **Action** and
   add more files. Every row shows its own action in the second column.

6. Press **Run all**.

## What happens next

Items run one at a time, from the top of the list down. The status line
names the item in progress ("[3/4] Verify: album.cue"), the row's Status
column reads `Running`, and rows below it still read `Pending`. The
progress bar along the bottom tracks the whole batch, not the current
item.

**Add file(s)...** and **Add folder...** keep working during a run, but
the batch was fixed when you pressed **Run all**: anything you add now
sits in the list and waits for the next run. **Clear** and **Run all**
are disabled until the batch ends. There is no Stop button on this page,
so a batch runs until every item in it has finished.

An item that fails does not stop the batch. Its row records the failure
and the next item starts.

Each row keeps what it reported. A later item never changes an earlier
one's verdict or detail, and the results stay on screen after the batch
ends. When every item is done, the status line reads
"Batch complete: 4/4 processed."

Queued verifies write the same two files a verify started from the
[Verify & Repair page](verify.md) writes: a dated `.accurip` report and a
`.toc` file listing the disc layout, both named after the file you queued
and both saved beside that album's audio. Queued conversions publish
their album folder the same way the Convert page does, all at once at the
end, so nothing half-written appears under the name you expect.

## Read the result

Each row has four columns: the file name, the action, the Status, and the
Result. Long text in the name and Result columns is trimmed with an
ellipsis; hover either one to see the full value. Rows show file names
only, so several discs called `album.cue` look identical in the list, and
the tooltip is what tells them apart.

The Status column carries one of these:

| Status | Action | What it means | What to do next |
| --- | --- | --- | --- |
| `Pending` | both | The item has not run yet in this batch. | Wait, or press **Run all**. |
| `Running` | both | This item is being processed now. | Wait. |
| `Verified` | Verify | A database confirmed this album: AccurateRip called the rip accurate, or CTDB returned a confidence above zero. | Nothing; the audio matches other people's rips. |
| `Repairable` | Verify | Neither database confirmed the album, and CTDB found damage it holds recovery data for. | Load that album on the [Verify & Repair page](verify.md) and repair it there. The queue does not repair. |
| `Lookup failed` | Verify | Neither database answered, so the run says nothing about your audio. The files were still read and checksummed. | Check your connection and queue the album again. The comparison is also retried automatically on a later launch; see [offline behavior and backfill](offline-and-backfill.md). |
| `No match` | Verify | At least one database answered, and none of them confirmed the album. If exactly one of the two never answered, its half of the Result column says so. | Read the Result column; it carries the reason each database gave. |
| `Failed` | Verify | The verify itself stopped before reaching a verdict. | Read the Result column for the error, fix what it names, and queue the album again. |
| `Done` | Convert | Every track was re-encoded and the album folder was published. | The Result column shows the file count, for example `24 m4a file(s)`. |
| `Failed` | Convert | The conversion stopped, and nothing was written. | Read the Result column for the reason. |

For a verify, the Result column carries the engine's own one-line summary,
which is where the database detail lives. In the batch below, a confirmed
album reads
`AR: offset 6, rip accurate (29/82), CTDB: verified OK, confidence 207`
and a damaged one reads
`AR: offset 6, rip not accurate (0/82), CTDB: differs in 7150 samples`.
Each line runs on past the width of the column, so hover it to read the
rest. When a database could not be reached, its half of the line says so:
`CTDB: database access error:` followed by the underlying error.

![The Queue page during a batch. Four rows are listed, all named album.cue, all with the action Verify. The first reads Repairable with the result "AR: offset 6, rip not accurate (0/82), CTDB: differs in 7150 samples, confidenc...", the second reads Verified with "AR: offset 6, rip accurate (29/82), CTDB: verified OK, confidence 207, or differ...", the third reads Running with no result yet, and the fourth reads Pending. The status line at the bottom reads "[3/4] Verify: album.cue" with the progress bar about half full. The Clear and Run all buttons are greyed out, as is the codec button reading "FLAC (.flac) - CUETools mana...".](2026-08-13-queue-batch-running.png)

*A batch in progress. Finished rows keep their verdicts while the third
item runs, and the buttons that would change the list are disabled until
the batch ends.*

![The same page after the batch. The first row still reads Repairable with the same result text, and rows two, three and four all read Verified with "AR: offset 6, rip accurate (29/82), CTDB: verified OK, confidence 207, or differ...". The status line reads "Batch complete: 4/4 processed.", the progress bar is full, and the Clear and Run all buttons are available again.](2026-08-13-queue-batch-complete.png)

*The finished batch, recorded on 2026-08-13. One album carries damage
CTDB can repair, and three reach the same reading: `rip accurate (29/82)`
and `confidence 207`.*

A batch of verifies is a practical way to check a set of copies after a
round of conversions. Converting between
[lossless](glossary.md#lossless) formats keeps the audio, not the bytes:
the new files are different sizes and different bytes from the source,
and decoding them returns the same samples. AccurateRip and CTDB compare
the decoded audio, which is why a converted copy reaches the same verdict
as the album it came from. See
[a lossless conversion keeps the audio, not the bytes](convert.md#a-lossless-conversion-keeps-the-audio-not-the-bytes).

## If something goes wrong

### An item reads `Failed` and `is a directory`

The item's source is a folder. Both actions read an album through its CUE
sheet or playlist rather than through a folder, so an item added with
**Add folder...** fails this way when the batch reaches it. The page's own
"Queue is empty - add album folders or .cue files above." message is
ahead of what the engine accepts.

Add the album's `.cue` or `.m3u` file with **Add file(s)...** instead. If
the album has neither, you can write a playlist yourself: make a plain
text file named `album.m3u` in the album's folder, listing the audio file
names one per line, in track order. Blank lines and lines starting with
`#` are ignored.

### An item reads `Failed` and "The queued codec is no longer ready."

The encoder that was pinned to that item when you added it cannot run
now, so nothing was attempted for it and the batch moved on. This is the
same readiness check the codec picker applies: an encoder program that is
not installed or did not pass its approval check, or a bundled library
that failed to load. The [Convert page](convert.md#the-format-you-want-is-greyed-out-in-the-codec-picker)
covers both messages.

Open the codec picker, choose a row that is not greyed out, and add the
album again.

### A verify item reads `No match` or `Lookup failed`

`No match` means at least one database answered and none of them
confirmed your disc. `Lookup failed` means neither answered, so the run
says nothing about your audio. The Result column carries the detail
either way: a database that answered and does not have your disc says
`disk not present in database`, while one that never answered says
`database access error:` with the reason.

If every item in the batch reads `Lookup failed`, check your network
connection. A verify run with no connection still
decodes every file and computes every checksum, and the database
comparison is queued to run again on a later launch, exactly as it is
from the Verify page. See
[offline behavior and backfill](offline-and-backfill.md).

### You want to remove one item, or stop a running batch

Neither is possible on this page. **Clear** empties the whole list, and
it is disabled while a batch runs. To drop a single item, press **Clear**
and add the ones you want back.

Pressing **Run all** on a finished list runs every item again from the
top, including the ones that already succeeded.

## Command line

```console
cuetools-linux --queue /path/to/album-one.cue /path/to/album-two.cue
cuetools-linux --queue --queue-run /path/to/album-one.cue /path/to/album-two.cue
```

`--queue` opens the app on the Queue page with each path added as an
item. Paths that do not exist are skipped without a message, so check the
list before running it.

Items added this way are always verifies, because **Action** starts on
`Verify` at every launch and nothing on the command line changes it.

`--queue-run` also starts the batch, and it needs `--queue` alongside it
and at least one path that exists.

## How it works

Each item runs through the same verify and convert services the
[Verify & Repair](verify.md) and [Convert](convert.md) pages use. The
queue adds the list, the order, and one verdict per item; it does not
have a checking path of its own, which is why a queued result and a
single-album result on the same disc read the same way.

A conversion item stores the identity of the encoder that was selected
when you added it, not the format alone. When the batch reaches that
item, the app looks that exact encoder up again and re-selects it before
encoding. Two items can therefore write FLAC through two different FLAC
encoders in the same batch, and an item whose encoder has since become
unavailable fails on its own rather than quietly encoding with a
different one.

## Related topics

- [Checking one album against AccurateRip and CTDB](verify.md)
- [Converting one album to another format](convert.md)
- [How Repair rebuilds a damaged album from CTDB parity](repair.md)
- [What happens when you verify offline, and how backfill catches up](offline-and-backfill.md)
- [Terms used in this manual](glossary.md)
