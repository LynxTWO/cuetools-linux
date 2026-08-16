# Rip

## What ripping means here

Every rip calibrates the drive first, and the result is checked against
AccurateRip and the CUETools Database before you are told anything
optimistic.

Corrected 2026-08-16: this section previously said "Every rip is a secure
read" and that "There is no fast-but-unverified mode hiding behind a
setting", which the Quality modes below contradict and the code does not
support. What every mode shares is calibration: `RipService.RunVerify`
(line 365), `RunEncode` (387), and `RunTestAndCopy` (1623) all call
`EnsureCalibration`. The independent re-read requirement is what varies.
Verify and Rip pass `requireIndependentReads: cq > 0`, so it binds only
above the lowest correction quality, while Test and Copy passes `true`
unconditionally. See needs-verification.md entry 1, and `pages/rip.md`
for the user-facing version.

## The page

Insert an audio CD and the page reads it on its own: drive identity in
the top bar, album and track list from MusicBrainz and the CUETools
Database, cover art when a release carries one. If more than one
release matches the disc, the release picker chooses which identity
names the output; pick a different one before ripping if the default
is wrong.

The right rail holds the choices that matter:

- **Output**: the codec (the same health-checked picker Convert uses),
  the output folder, cue sheet and log toggles, and the layout - one
  file per track, or a single image with an embedded CUE.
- **Quality**: Burst, Secure, Paranoid, or Salvage. Secure is the
  default: every window is read at least twice and must agree. Paranoid
  reads deeper before giving up. Burst is a single pass with the
  historical retry cap - still not blind, but without deep recovery.
  Salvage exists for discs that are defective by design; it captures at
  Burst quality with error pointers off at the drive's minimum speed,
  and its output is labeled salvaged, never verified.
- **Run**: Rip encodes and publishes. Verify only reads and checks
  without writing files. Test & Copy is the strictest: two full
  independent reads (the first purely as evidence, the second encoded),
  which must agree track by track before anything is published.

## What you see while it reads

The disc map fills from the center outward exactly as far as the read
has progressed - a CD is read inside-out, and most of the data sits
near the rim. The meters show the music's loudness (RMS, per channel)
and the read speed as separate things: when the drive slows to fight a
bad patch, the speed trace dips while the level meters hold the last
music.

When a window refuses to agree with itself, the re-read box appears
with the literal counts: how many extra passes this spot has taken, and
how many sectors still disagree. The sweep slows as the count climbs
because the real drive slows too. It turns green the moment the sectors
agree; if the drive finally gives up, that window is recorded as
unreadable rather than quietly papered over - the counts in the log and
the final verdict say exactly which windows those were.

## Reading the result

A finished rip reports the same honest vocabulary as Verify: AccurateRip
confidence against worldwide rips, CUETools Database confidence with
parity-repair detail when damage was found, and the per-track evidence
grid. Test & Copy additionally publishes each phase's CRC evidence as
it completes - the Test column fills before Copy starts, and a later
phase never erases an earlier one's numbers.

Damaged discs stay truthful: matching reads with unrecoverable windows
are CONSISTENT, not cleanly verified, and a repairable disc says so with
the exact sample counts the database's parity can reconstruct. The
Repair flow in Verify & Repair can then rebuild a verified copy without
touching the rip you made.

If a Test & Copy confirmation fails after the Copy finished encoding,
the copy is kept in an explicit Held state - not published, not
deleted - because it may be the only complete read of a dying disc. The
Held panel offers the honest choices.

## Multiple drives

Each drive works in its own window. While a rip runs, picking another
attached drive in the selector opens that drive in a separate isolated
CUETools window with its own Stop, status, and evidence; the running
window's drive can never be retargeted mid-job. Two windows can never
claim the same physical drive - the second claimant is refused before
the hardware is touched.

## When a drive misbehaves

Optical drives have firmware quirks, and this app's policy is to catch
them exactly rather than guess. Known quirks are handled per exact
drive model and firmware; anything unrecognized fails closed with the
full diagnostic identity (command, sector, sense data) in the log. If a
USB drive stops accepting commands entirely - it can happen under heavy
error-recovery on damaged discs - unplugging and replugging its cable
resets it; the rip that was running stays honestly failed rather than
silently degraded.
