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

## The real damaged disc, end to end (2026-08-12 walkthrough)

The owner supplied a scratched 24-track CD (1:08:22). The walkthrough ran
on Linux with the --repair launch flag; every number below is from the
published receipt (repair.verify) and the diagnostic log.

1. **Rip.** All 24 tracks ripped to WAV with GStreamer cdparanoiasrc.
   Track 1 took 47 seconds against 3-30 seconds for the rest - the drive
   fighting the scratch is visible before any database is consulted.
2. **Verdict.** AccurateRip: "no match | 0/82" - 82 clean rips of this
   pressing exist and the damaged rip matches none of them. CUETools DB:
   "damage found | parity available". The card reads REPAIRABLE.
3. **The damage, measured.** 7,150 damaged samples across 129 of 307,669
   sectors, concentrated in the disc's outer third (the ranges run from
   41:29 to 66:37). Parity headroom: the worst 10-sector stripe used
   **4 of 4** correctable errors - this disc sat at the exact limit of
   what npar=16 parity can recover. One more damaged sector in that
   stripe and CTDB could not have repaired it.
4. **Repair.** 88 seconds from confirmation to published: reconstruct
   from parity, encode the sibling copy, verify it independently, seal
   evidence, publish atomically.
5. **Proof.** The repaired copy verifies as AccurateRip **accurate,
   confidence 29 / 82** and CTDB **exact match, confidence 207 / 235**.
   The rip that matched nothing now matches the community's clean rips
   bit for bit. The 25 source files (24 WAV + cue) re-hashed SHA-256
   identical to the pre-repair snapshot - measured, not assumed.

Screenshots: 2026-08-12-repair-real-disc-repairing.png (REPAIRING with
the damage map showing the scratch's real geography),
2026-08-12-repair-real-disc-verified.png (Repaired copy verified).

## What the repaired copy contains (from the real output)

A sibling folder next to the source cue, named `<stem> - repaired`:

- 24 FLAC tracks keeping the source-derived basenames (track01.flac for
  track01.wav).
- `album.cue` rewritten for the new files.
- `album.accurip` - a fresh named AccurateRip report for the repaired
  audio (not the source's stale one).
- `album - CTDB Repair.log` - the human-readable repair report:
  corrected samples/sectors, parity headroom, corrected ranges, both
  database verdicts, and the preservation statement.
- `repair.verify` - the machine receipt (CUETOOLS_REPAIR_RECEIPT_V1):
  repair scope, both confidences, and SHA-256 proofs for all 25 source
  files and all 25 output files.
- `.cuetools-complete` - written last; its presence marks a fully
  published transaction.

## Failure leaves nothing behind (also observed for real)

An earlier run of the same walkthrough hit a genuine bug (evidence
sealing failed under the AOT runtime; fixed in the fork the same night).
The transaction failed 18 times and the process was then killed
mid-flight. After all of that: the source files re-hashed byte-for-byte
identical, no partial sibling was published, and each failed attempt
removed its own staging directory. The preservation contract held under
repeated failure, which is worth more than holding when everything goes
right.
