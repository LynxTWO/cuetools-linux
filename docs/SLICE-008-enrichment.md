# CUETools Linux Slice Brief: SLICE-008 Enrichment

Version: 0.1 Draft. Date: 2026-08-13. Status: Approved for build (the
owner's 2026-08-13 widget rounds answered this slice's design questions:
D-048 preview-diff per album, D-049 artwork embed + folder.jpg with the
size cap; built under the standing autonomy grant).
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md,
SLICE-001-verify.md (D-010/D-011 journal design).

## 1. What the slice adds, and why it is next

- **Capability:** metadata enrichment with owner-approved application: an
  album with missing or thin metadata gets a database lookup (CTDB
  metadata search, extending to gnudb per the engine's Extensive mode),
  the result is presented as a before/after diff of every proposed change,
  and one approval per album applies it. Nothing is ever written without
  the diff being shown and approved (D-048).
- **Why now:** it is the roadmap's next non-rip surface (D-043/D-047
  predecessors shipped), every design question is answered, and the
  walkthrough albums themselves demonstrate the need: the repaired album
  verifies bit-exact yet displays as "Unknown Artist - Unknown Album".

## 2. Increments

- **A - tag enrichment (this build):** EnrichmentService in the Linux
  app (the journal precedent: app-side first, promoted to the shared
  core when the WPF head adopts it; it uses only public engine APIs):
  Propose(path) opens the album, runs the engine's
  LookupAlbumInfo, and diffs the best release candidate against the
  album's current metadata (album artist, album title, year, genre,
  per-track titles). Apply(proposal) writes approved fields into the
  audio files' tags via the engine's own Tagging.Analyze/UpdateTags
  path. The Linux Verify page's disc card gains "Enrich metadata...";
  the dialog is the preview diff with one approve per album. The source
  .cue text file is NOT rewritten in increment A (recorded debt).
- **B - offline journal lane:** a lookup attempted offline journals an
  enrichment-pending entry (D-010's second lane); on a later online
  launch the pending albums surface for the same propose/diff/approve
  flow. Proposals are always generated fresh at approval time, never
  stored - determinism comes from the journal recording WHAT needs
  enrichment, not what the answer was.
- **C - artwork:** cover fetch with embed + folder.jpg per D-049,
  behind the same diff/approve gate, with an Avalonia image pipeline
  (the WPF AlbumArtService's imaging is head-specific).

## 3. Acceptance criteria

| ID | Criterion | Verified by |
| --- | --- | --- |
| S8-001 | Propose produces an honest field diff (current vs proposed) and proposes nothing when the album already matches | Tests |
| S8-002 | Apply writes exactly the approved fields to the audio files' tags and nothing else; declining writes nothing | Tests + evidence |
| S8-003 | The dialog shows every change before any write (preview-diff per album, D-048) | UI + walkthrough |
| S8-004 | A real album (the walkthrough's "Unknown Artist" set) enriches to its true metadata on Linux | Evidence run |
| S8-005 | (B) Offline lookups journal and surface later | Increment B |
| S8-006 | Owner walkthrough | Owner sign-off |

## 4. Verification evidence required

- [x] Increment A tests green (ApplyWritesExactlyTheApprovedFields incl.
      unapproved-field-untouched, ApplyWithNoChangesWritesNothing; suite
      46/46).
- [x] Real-album enrichment evidence run (2026-08-13): the repaired
      album's scratch copy - the set that verified bit-exact for two days
      as "Unknown Artist - Unknown Album" - looked itself up and applied
      **27 changes across 24 files from MusicBrainz** (via the CTDB
      metadata search): album artist, album title, year, and per-track
      titles all now set (verified in the tags without logging the
      values; the owner's listening data stays out of the public
      record). Run under the --enrich command-line consent flag (the
      --repair precedent); the interactive path shows the
      EnrichmentDialog preview diff with one approve per album.
- [ ] EDD section 17 per-change checklist per change.

---

*Design questions answered by: Daniel Boyd, 2026-08-13 (D-048/D-049);
built under the standing autonomy grant.*
