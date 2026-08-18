# Uncommitted packaging work destroyed 2026-08-17, and what was recovered

## What happened

While moving a documentation commit off an already-merged branch, I ran
`git reset --hard origin/master` on `feat/first-run-artwork-consent`. The
branch had been merged as PR #58, so the intent was to return it to master.
`--hard` also discards working-tree changes, and the owner's uncommitted
packaging work was in that working tree. It was reverted to master's
versions.

The work had never been staged, so git holds no copy: `git fsck
--unreachable --dangling` returned four blobs, none of them these files.
`git stash` was empty. There are no filesystem snapshots, no VS Code local
history for this repository, and no editor backup files.

The reset was avoidable. `git status` had been showing these four modified
files all session, and I had read every one of them minutes earlier.

## Recovered exactly

Both were reconstructed from the complete diffs printed during the review
earlier that day. The restored insert counts match the originals exactly,
32 and 23 lines, which is the check that they are complete rather than
approximate.

- `eng/package-deb.sh` - the `native/` and `encoders/` payload loop, the
  `assert-payload.sh` call, the copyright note about third-party binaries,
  and the `collect-third-party-notices.sh` call.
- `eng/package-appimage.sh` - the same payload loop against `$APPDIR`, its
  `assert-payload.sh` call, and the notices call into
  `usr/share/doc/cuetools-linux`.

## Never lost

Untracked files are not touched by `git reset --hard`, so both new scripts
survived intact and are unchanged:

- `eng/assert-payload.sh` (74 lines)
- `eng/collect-third-party-notices.sh` (131 lines)

## Lost, and only partly reconstructable

CLOSED 2026-08-18: both files below were reconstructed at the owner's
request (PR #67). The provenance table turned out to be fully captured
and is restored verbatim, corroborated by the surviving collector's
licence file names and its manifest reads. The README keeps every
captured sentence verbatim and updates only facts that changed after the
loss. The section below stays as written, as the record of what the
fragments were.

Both were reviewed with truncated output, so only fragments are available.
Neither file has been touched: both sit at master's version, so nothing
half-restored is masquerading as complete. The fragments below are what the
review captured, for the owner to rebuild from.

### `eng/build-native-codecs.sh` (was 45 lines changed)

A `PROVENANCE` table so the third-party notices generate from the manifest
rather than a hand-maintained list. Values were stated to match the fork's
`eng/release/native-dependencies.json`, which is the pin of record and
should be treated as the source for rebuilding it. The captured fragment:

```python
# Version, licence, and upstream source per built library, so the shipped
# third-party notices are generated from the manifest rather than from a list
# somebody has to remember to update. Values match the fork's
# eng/release/native-dependencies.json, which is the pin of record.
PROVENANCE = {
    'libFLAC_dynamic.so': {
        'component': 'libFLAC',
        'version': '1.5.0',
        'license': 'BSD-3-Clause',
        'source': 'https://github.com/xiph/flac',
    },
    'wavpackdll.so': {
        'component': 'WavPack',
        'version': '5.9.0',
        'license': 'BSD-3-Clause',
        'source': 'https://github.com/dbry/WavPack',
    },
    'MACLibDll.so': {
        'component': "Monkey's Audio SDK",
        'version': '13.20',
        'license': 'BSD-3-Clause',
        'source': 'https://monkeysaudio.com/developers.html',
    },
    'libmp3lame.so': {
        'component': 'LAME',
        'version': '3.100',
        'license': 'LGPL-2.0-or-later',
        'source': 'https://lame.sourceforge.io/',
    },
```

The entry list is cut off after LAME. `collect-third-party-notices.sh`
survived and names the same components, so its `copy_license` calls are a
reliable guide to which entries the table needs.

### `README.md` (was 101 lines changed)

A rewrite from "designed, not yet built" to "built, unreleased, and not yet
recommended for strangers", with a "What works today" table and a "Known
limitations" section. Captured fragments:

Opening paragraph:

> A native Linux desktop app bringing the CUETools 2026 experience to Linux:
> rip CDs with calibrated assurance, verify rips against AccurateRip and the
> CUETools Database, repair damaged rips from CTDB parity, and convert between
> lossless formats. Same engine as the Windows build, same evidence discipline,
> in a package a fraction of the size.

Status paragraph:

> **Status: built, unreleased, and not yet recommended for strangers.** Eleven
> slices are complete with evidence, from verification through ripping. There
> is no published release yet, and several things a first-time user needs are
> still missing. See [Known limitations](#known-limitations) before trying it.

The "What works today" table, complete as captured:

| Capability | State |
| --- | --- |
| Verify a rip against AccurateRip and CTDB | Built, evidenced |
| Repair a damaged rip from CTDB parity | Built, evidenced on a real scratched disc |
| Convert between lossless formats | Built, evidenced across FLAC, ALAC, APE, WavPack |
| Batch queue for verify and convert | Built, evidenced |
| Rip a CD, including secure and Test and Copy modes | Built, evidenced on three drives |
| Metadata and cover-art enrichment | Built, evidenced |
| Offline journal and automatic backfill | Built, evidenced |

Followed by:

> Every claim above traces to a slice brief in `docs/` with its own evidence
> section. Nothing here is inferred from a screenshot.

And the "Known limitations" heading with its lead-in:

> Read these before installing. They are the reason there is no release yet.

Three fragments of its bullets survive, without their opening words:

> glibc 2.38 or newer, so it does not start on Ubuntu 22.04 or Debian 12
> despite those being the intended floor. Fix in progress.

> from the interface. Output naming follows a fixed template, and the
> stop-on-unrecoverable safety switch can only be changed by editing
> `~/.config/CUETools2026/settings.txt` while the app is closed.

The rest of that section, and whatever followed it, was not captured.

## What changes so this cannot repeat

`CLAUDE.md` in this repository already said never to run `git add -A`,
because the owner keeps uncommitted work in the tree. The rule was about
the wrong command. It now covers any command that discards working-tree
state, and requires checking `git status` first when the tree is not clean.
