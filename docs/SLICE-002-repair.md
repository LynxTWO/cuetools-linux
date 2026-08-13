# CUETools Linux Slice Brief: SLICE-002 Repair

Version: 0.1 Draft. Date: 2026-08-12. Status: Approved for build (owner
selected 2026-08-12; SLICE-001's remaining owner rows S-007/S-008 ride in
parallel by explicit owner choice, D-036).
Companion documents: ARCHITECTURE.md, ENGINEERING.md, DECISION-LOG.md,
SLICE-001-verify.md.

## 1. What the slice adds, and why it is next

- **Capability:** CTDB parity repair inside the Verify & Repair page: a
  disc with database-repairable damage gains a Repair action that builds an
  independently verified sibling copy, never touching the source.
- **Why now:** it completes the page's own name. The engine's
  preservation-transaction machinery (CueRepairEngine, RepairWorkspace,
  RepairEvidence receipts) already ships in the shared app core, and
  VerifyViewModel carries RepairDiscCommand wired end to end; SLICE-001
  deliberately omitted only the UI surface (D-032). Repair is CUETools'
  most distinctive capability and the smallest next step with the largest
  promise kept.

## 2. The walkthrough

1. User verifies an album; one disc reports damage that CTDB parity can
   recover. The disc card's outcome reads REPAIRABLE and a "Repair this
   disc" action appears (exactly the WPF affordance).
2. User triggers repair; the confirmation states the contract: a new
   sibling folder, independent verification, source files untouched.
3. Repair runs with progress; the CTDB parity panel presents the repair
   scope (samples, sectors, parity headroom) in the 2026 identity, with
   the RepairScope visualization ported per the disc-read-visualization
   skill's portable pattern.
4. On success the card flips to REPAIRED + VERIFIED, shows the output
   path, and the repaired sibling carries its own dated evidence; the
   source remains byte-for-byte unchanged. On failure the source is
   untouched and the card says so plainly, keeping the completed
   verification evidence.

## 3. In scope

| Item | Notes |
| --- | --- |
| Repair action + confirmation on the disc card | VerifyViewModel.RepairDiscCommand already exists; UI blocks port from the WPF VerifyView |
| CTDB parity repair panel (summary, headroom, ranges, output path) | WPF blocks omitted in SLICE-001 return |
| RepairScope visualization | Ported per the disc-read-visualization skill; may degrade to a simpler presentation with a logged note if it balloons |
| Repaired-output evidence surfaced | The engine's receipts and proofs, shown honestly |
| Headless tests for the repair flow | Fake IVerifyService.Repair paths: success, failure, confirmation declined |

## 4. Out of scope, on purpose

| Excluded | Where it connects later | Log entry |
| --- | --- | --- |
| Rip, Convert | Unchanged from SLICE-001 exclusions | D-032 |
| Enrichment backfill | Journal enrichment lane | D-011 |
| Native codec runtime | Codec registration seam (A-003 spike belongs there) | D-032 |
| Repair of sources needing native codecs | Arrives with the codec slice | D-036 |

## 5. Stubs and their debts

None new. SLICE-001's stubs stand.

## 6. Modules touched

Desktop shell (VerifyView repair blocks, RepairScope port), Shared app
core (consumed; no fork changes were expected for the flow itself, but
the real-disc walkthrough found one: RepairEvidence.ToJson used
reflection-based System.Text.Json, which the AOT runtime rejects at
evidence sealing. Fixed in the fork with a source-generated
JsonSerializerContext, fork PR #13), Job orchestration (journal
untouched; repair is online-interactive by design: parity requires the
database, so an offline repair simply is not offered).

## 7. Data subset

No new app-owned entities. Repair evidence uses the engine's formats.

## 8. Acceptance criteria

| ID | Criterion | Verified by |
| --- | --- | --- |
| S2-001 | Given a disc verdict with recoverable damage, when the user confirms Repair, then a sibling copy is produced that independently verifies clean, and the source directory's bytes are unchanged (hash comparison) | Integration evidence run + hash proof |
| S2-002 | The repaired output carries the engine's named repair evidence, and the UI shows outcome, output path, and repair scope | Evidence run + screenshot |
| S2-003 | Discs without recoverable damage never show the Repair action; declining the confirmation runs nothing | Headless tests |
| S2-004 | A failed repair leaves the source untouched, keeps the prior verification evidence, and says so plainly | Headless test + evidence run |
| S2-005 | Repair honors the fail-closed confirmation seam (no prompt service means no repair) | Existing core behavior, pinned by test |
| S2-006 | Real-album walkthrough: an owner-provided CTDB-covered album, deliberately damaged copy, repaired and verified on Linux | Owner-assisted evidence run |

## 9. Verification evidence required

- [x] Headless tests for S2-003/S2-004/S2-005 passing in CI.
- [x] A real damaged-disc repair evidence run for S2-001/S2-002/S2-006
      (owner-provided scratched CD, 2026-08-12). Receipt: repair published
      with 7,150 samples corrected in 129 sectors, worst stripe 4 of 4
      correctable errors used, AccurateRip confidence 29/82 and CTDB
      207/235 on the repaired copy, all 25 source files SHA-256 unchanged
      (snapshot taken before the first attempt, re-checked after
      publication). Screenshots:
      docs/evidence/2026-08-12-repair-real-disc-repairing.png,
      docs/evidence/2026-08-12-repair-real-disc-verified.png. Bonus
      finding: the walkthrough exposed and fixed a real AOT serialization
      bug in evidence sealing (fork PR #13) plus a retry-loop bug in the
      --repair driver (one-attempt-per-disc guard, pinned by test).
- [ ] EDD section 17 per-change checklist per change.

## 10. Agent guardrails for this build

Boundary per section 6; D-028 stop list; mode separation; conflicts stop
and surface. SLICE-001's owner rows (S-007/S-008) remain open and owned by
the owner in parallel.

## 11. Slice definition of done

All acceptance criteria evidenced; documents updated; owner walkthrough
approval.

## 12. What this unlocks

- Codec runtime slice (WavPack/APE verification and repair; closes A-003).
- Convert slice (cue-fidelity conversion with the encoder catalog seam).

---

*Approved for build by: Daniel Boyd, 2026-08-12 (slice selection).* 
