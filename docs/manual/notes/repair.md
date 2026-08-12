# Repair (CTDB parity)

## The contract, in one paragraph

When verification finds damage that the CUETools Database can recover
(the disc card reads REPAIRABLE and the CTDB panel says "damage found |
parity available"), the Repair action builds a NEW sibling copy of the
album, verifies the repaired audio independently, and publishes it only
if that verification succeeds. The selected source files are never
changed - repair is a preservation transaction, not an edit.

## The flow

1. "Repair this disc" appears on a repairable disc card only; discs
   without recoverable damage never show it.
2. The confirmation states the contract before anything runs. Declining
   runs nothing. (Headless environments without a prompt cannot confirm,
   so repair simply does not run there - fail-closed by design.)
3. During repair the CTDB parity panel shows the real numbers: corrected
   samples, damaged sectors, parity depth (npar per 10-sector stride),
   and the affected ranges.
4. Success flips the card to REPAIRED + VERIFIED and shows the output
   path. Failure leaves the source untouched, keeps the completed
   verification evidence, and says "Repair failed ... The completed
   verification evidence was kept."

## Reading the RepairScope

The animated strip is the disc, inside (left) to outside (right), drawn
from the real per-sector damage map:

- Amber-to-red marks: damaged sectors, color-graded by how dense the
  damage is at that spot - a scratch shows as a cluster exactly where it
  is on the disc.
- During repair, a green sweep tracks the real progress; marks turn green
  as their sectors are reconstructed.
- All green plus the REPAIRED pill: done.
- The five chips underneath are the actual Reed-Solomon decode stages
  (syndrome, locate, Chien, Forney, apply). The first four light during
  verification, because that is when the math actually runs; "apply"
  lights only when a repair writes the corrected samples.

Every number shown is the literal value from the repair engine; only the
animation is smoothed.

Screenshot: 2026-08-12-repairscope-states.png (three states).

## Not yet written (capture during the damaged-disc walkthrough)

- The end-to-end story with a real damaged disc: rip, REPAIRABLE verdict,
  repair, verified sibling copy, before/after hashes.
- What the repaired copy contains (file naming, preserved tags/artwork,
  repair evidence files) - document from the real output, not from
  intention.
