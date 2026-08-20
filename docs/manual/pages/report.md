# The Report page, the last job's certificate

The **Report** page shows the certificate for the most recent rip or
verify: one headline verdict, the album it belongs to, the two database
answers, and the full rip log. It reads what the job already wrote and
writes nothing itself, so you can open it at any time without touching
your files.

Before the first job of a session finishes, the page says **No report
yet**, with one line telling you where reports come from: "Run a Verify
or Rip on the Rip page and the accuracy certificate lands here." The
page fills in the moment a job completes, and each later job replaces
the certificate with its own.

## The headline

The headline is the whole result in one phrase. One row per distinct
message:

| Headline | What it means |
| --- | --- |
| **Accurately ripped** | The rip matched other people's rips in a database (AccurateRip or CTDB). |
| **Verified by independent reads** | No database confirmed it, but the drive read the disc several times and every track agreed across the required number of reads. The evidence line at the bottom carries the counts. |
| **Consistent - damage recorded** | The reads agreed with each other, but some sectors could not be read at all. The log records exactly which ones. |
| **Salvaged capture - damage recorded** | A Salvage-mode capture with unreadable sectors: the audio is the best the disc would give, and the damage is listed. |
| **Salvaged capture - not verified** | A Salvage capture with no database match and no independent confirmation. |
| **Not confirmed** | The job finished, but nothing confirmed the audio: no database match, no agreeing re-reads. |

A damaged rip never gets a verification headline, whatever else
matched: the certificate never says more than the log. The wax seal
next to the headline is green when the result is confirmed and amber
otherwise.

## The two database cards

The cards report each database's answer separately, because they are
separate databases with separate submissions:

- **ACCURATERIP** shows `confidence N` when your rip matched (N people
  ripped the same bytes), `N / M` when the disc is known but your rip
  matched only some pressings' counts, and `not in database` when the
  disc has no entry.
- **CUETOOLS DB** shows `confidence N` for a match, and `not found`
  otherwise.

[Confidence](glossary.md#confidence) is the number of other rips yours
agrees with, not a percentage.

## The log and the checksum

**RIP LOG** is the full text the job wrote, selectable so you can copy
it. Under it, the integrity line explains the digest printed in the
footer: a checksum calculated over the log text above it. It is not a
signature; it is a number you can keep, so that if you save the
certificate today and read it again next year, you can tell whether the
text changed. The same line names the read evidence in plain terms
(which databases confirmed the rip, or how many optical reads agreed),
and whether the final encoded output was decoded and compared after
tagging.

## Related topics

- [Ripping a CD](rip.md)
- [Verifying existing rips](verify.md)
- [Terms used in this manual](glossary.md)
