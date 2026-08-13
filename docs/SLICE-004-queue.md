# CUETools Linux Slice Brief: SLICE-004 Queue

Version: 0.1 Draft. Date: 2026-08-13. Status: Proposed (agent-selected
under the owner's overnight autonomy grant, D-041; owner confirms or
vetoes in the morning).
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md,
SLICE-003-convert.md.

## 1. What the slice adds, and why it is next

- **Capability:** the batch Queue page: stack album folders or cue
  sheets, choose Verify or Convert per batch, and run them in one
  sitting. Each item carries its own honest status (Verified /
  Repairable / No match / Done / Failed with the reason); the codec
  chosen for a queued conversion is pinned by stable implementation id
  when the item is enqueued, and a codec that is no longer ready when
  the batch runs fails that item plainly instead of substituting.
- **Why now:** the mass use case. Verifying or converting one album at a
  time is the demo; a collection is the reality. Everything underneath
  (verify service, convert service, encoder catalog) shipped in
  SLICE-001..003; the queue is the thin surface that multiplies them.

## 2. The walkthrough

1. User adds album folders or cue files to the queue (dialogs or startup
   arguments), picks the action (Verify or Convert) and, for Convert,
   the output codec.
2. Run all: items execute one at a time; the running item shows live
   progress, finished items keep their per-item verdict and detail.
3. The batch summary states what was processed; nothing is erased.

## 3. In scope

| Item | Notes |
| --- | --- |
| Fork extraction of QueueViewModel + QueueItem | Fork PR #18 (M2 recipe: dialog + dispatcher seams, EnqueuePath for arguments/tests) |
| Queue page in the Linux app | Port of QueueView in the 2026 identity |
| Codec pinning honesty | The queued StableId is re-resolved at run time; a not-ready codec fails that item with the reason |
| Headless tests | Enqueue rules, per-item verdicts, not-ready-codec failure, batch completion |
| Batch evidence run | A real multi-album batch (verify + convert) on Linux |

## 4. Out of scope, on purpose

| Excluded | Where it connects later | Log entry |
| --- | --- | --- |
| Parallel item execution | Sequential is the WPF behavior; parallelism is its own review | D-041 |
| Rip jobs in the queue | Rip slice | D-032 |
| Enrichment backfill | Journal enrichment lane | D-011 |

## 5. Stubs and their debts

None new.

## 6. Modules touched

Fork shared app core (queue moves in; WPF head unchanged), Desktop shell
(Queue page, nav entry), Composition (QueueViewModel wiring on the
existing services).

## 7. Data subset

No new app-owned entities; the queue is session state (the WPF page does
not persist it either).

## 8. Acceptance criteria

| ID | Criterion | Verified by |
| --- | --- | --- |
| S4-001 | Multiple albums queue and run in one sitting; each item ends with its own verdict and detail, and the batch summary is accurate | Evidence run + screenshot |
| S4-002 | A queued conversion pins its codec by stable id; a codec no longer ready at run time fails that item plainly and the batch continues | Headless test |
| S4-003 | Verify items report the same verdict vocabulary as the Verify page (Verified / Repairable / No match / Failed) | Headless test |
| S4-004 | Remove/Clear are blocked while a batch runs; finished item state is never erased by a later item | Headless test |
| S4-005 | Real-batch walkthrough on Linux with owner-relevant albums | Owner-assisted evidence run (queued for morning) |

## 9. Verification evidence required

- [x] Headless tests for S4-002/S4-003/S4-004 passing in CI (three
      QueueFlowTests; suite 31/31).
- [x] A real multi-album batch evidence run for S4-001 (2026-08-13): a
      4-item batch verify of the damaged rip and its three descendants
      (repaired FLAC, converted ALAC, round-trip FLAC). Result: the
      damaged rip **Repairable** (rip not accurate 0/82, CTDB differs in
      7150 samples), and all three derived albums **Verified** (rip
      accurate 29/82, CTDB confidence 207) - three codecs, three
      conversion generations, all bit-identical to the community's clean
      rips. "Batch complete: 4/4 processed." Screenshots:
      docs/evidence/2026-08-13-queue-batch-running.png,
      docs/evidence/2026-08-13-queue-batch-complete.png.
- [ ] EDD section 17 per-change checklist per change.

## 10. Agent guardrails for this build

Boundary per section 6; D-028 stop list (no new dependencies); mode
separation; conflicts stop and surface. The slice selection itself is
provisional (D-041) until the owner's morning review.

## 11. Slice definition of done

All acceptance criteria evidenced; documents updated; owner confirmation
of D-041 plus walkthrough approval.

## 12. What this unlocks

- Codec-runtime slice (pending the owner's native-dependency approval).
- Rip slice: a rip job is a queue item whose source is a drive.

---

*Selected provisionally under the overnight autonomy grant (D-041);
owner review pending.*
