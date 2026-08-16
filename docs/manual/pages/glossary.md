# Glossary

Terms used across this manual. Each entry explains the idea first, then
the detail you need if you want it.

## AccurateRip

A public database of CD rip checksums, submitted by people using ripping
software over many years. It stores per-track checksums for a disc, keyed
by that disc's track layout. When your checksums match theirs, independent
copies of the same disc agree with yours.

## AppImage

An AppImage is a single executable file that holds a whole Linux
application, so there is nothing to install. You make the file executable
and run it; deleting the file removes the application.

An AppImage carries the libraries an application cannot expect to find on
every system, which is why it is larger than a distribution package. It
still uses the basic system libraries, so it has the same distribution
requirements as the `.deb` built from the same application.

## backfill

Backfill is CUETools running a database lookup later, because the run that
needed it had no network. An offline verification still finishes locally,
and the album goes into a queue; a later launch works through that queue
and asks the databases then.

The verification queue does not replay a stored answer. It verifies the
album again from scratch, so what you end up with is a complete, dated
verification rather than a patched one. See
[offline behavior and backfill](offline-and-backfill.md).

## cache defeat

Reading an unrelated part of the disc so that the drive's own memory has
to let go of the part CUETools actually wants, before that part is read
again. Without it a second read can be answered from the drive's cache,
and two reads that never reached the disc twice would agree no matter
what happened at the surface.

CUETools measures each drive during [calibration](#drive-calibration): it
records whether the drive answers re-reads from memory, and how many
bytes have to be read elsewhere to clear it. Secure and Paranoid use that
figure on a drive that caches, and Test & Copy will not start at all
unless the drive proved a re-read can reach the disc.

## confidence

A count of how many independent submissions agree with your audio.
Higher numbers mean more independent agreement. Confidence is not a
percentage or a score, and a low number on a rare disc is normal.

The album-level AccurateRip figure reports the disc's weakest track, so
`accurate | confidence 4` means every track agrees with at least four
other rips. The CTDB figure adds up the submissions behind every
database entry that matches you, so it is usually a much larger number.

## CRC32

A short number computed from a block of data, used to tell whether two
blocks are identical. Two tracks with the same CRC32 are almost certainly
the same audio; two with different CRC32 values are definitely different.

CUETools shows a CRC32 for each track so you can compare rips by eye or
against another tool. The databases are not queried with this number:
AccurateRip and CTDB each use their own checksum, computed the way that
database expects.

## CTDB (CUETools Database)

A public database that stores checksums and recovery data for CD rips.
The recovery data is what makes repair possible: when your audio nearly
matches a known entry, CTDB can locate the damaged parts and rebuild
them. See [parity](#parity) and [Reed-Solomon](#reed-solomon).

## drive calibration

A short set of read-only measurements CUETools makes of your optical
drive before it rips with it for the first time. It records the drive's
speed range, whether the drive answers a re-read from its own memory
instead of the disc, and how much has to be read elsewhere to clear that
memory. Nothing is written to the disc, and the audio is not affected.

The result is saved per drive, so it happens once rather than every rip,
and a Rip, Verify, or Test & Copy refreshes it when it is missing or out
of date. It needs the audio disc in the drive. Secure and Paranoid
reading depends on it: without a proven way to make a re-read reach the
disc, they refuse to start rather than compare a read with itself. See
[cache defeat](#cache-defeat).

## glibc

glibc is the GNU C Library, the part of a Linux system that almost every
program uses to reach the kernel. Which version you have is decided by your
distribution release, not by the programs you install.

A program compiled against a newer glibc will not start on a system with an
older one, which is why an application can require a minimum distribution
release. The requirement runs one way: a newer glibc runs older programs
without trouble.

## independently verified

A repaired copy is checked again from scratch, as if you had loaded it
yourself, before CUETools keeps it. The repair math finishing is not
treated as success: after repair writes the new files, CUETools runs a
fresh verification on them and asks both databases about the result. The
repaired copy is published only when AccurateRip or CTDB confirms it.

## lossless

A format that stores audio without discarding any of it: decode a
lossless file and you get back exactly the samples that went in. FLAC,
Apple Lossless, WavPack, Monkey's Audio, and plain WAV are all lossless.

Lossless formats differ in how hard they compress and how widely players
support them, not in what they keep. Converting from one to another
changes the file, never the audio inside it.

## lossy

A format that makes files much smaller by throwing away detail it judges
you will not hear. MP3, Opus, Ogg Vorbis, and Musepack are lossy, and
what they discard cannot be recovered.

A lossy copy is a derivative, not a master. Keep the lossless album, make
lossy copies from it, and remake them if you later want a different
setting.

## parity

Extra data stored alongside a recording that can rebuild missing or
damaged parts of it. Parity has a limit: it can rebuild damage up to a
certain amount and no further, so a badly damaged disc can be beyond
repair even when the database knows it.

CTDB holds parity for many of the discs it knows, but not all of them.
An entry without parity can still confirm a clean rip; it just cannot
repair a damaged one.

## parity stripe

The unit CTDB's recovery data is divided into. CTDB lays a disc's
[parity](#parity) over a repeating 10-sector pattern, and each position
in that pattern can rebuild only a limited number of damaged values.

The CTDB parity repair panel reports this as `worst stripe 4/4`: the most
heavily damaged position needed 4 corrections, and 4 were available to
it. CTDB fetches parity at increasing depth and stops at the first depth
that can rebuild the disc, so the second number describes the depth this
repair settled on rather than everything the database holds for the disc.

## PCM (pulse-code modulation)

The raw, uncompressed form of digital audio: one number per channel per
sample, 44,100 samples a second for a CD. Every codec decodes to PCM and
encodes from it, so PCM is the common ground any two formats meet on.

The Convert page's round-trip display names it directly. The source
unpacks to PCM in the middle card, and the target packs that same PCM
again on the right, which is why a lossless conversion can change the
file without changing the audio.

## pressing

One manufactured version of an album. The same album can be pressed
several times, and different pressings can carry slightly different audio
or track layouts.

Neither database is organised by pressing. Both look a disc up by its
track layout, then compare checksums, so two pressings that share a
layout land on the same lookup and disagree on the audio. That is why a
disc can be "known" while your particular copy matches nothing.

## read offset

The small fixed shift, measured in samples, between where a CD drive
starts reading audio and where the disc actually begins. Drives do not
all start at the same point, and each model has its own shift.
Correcting for it is what lets rips from different drives match each
other exactly.

Offsets are small for most drives and large for a few. The AccurateRip
drive offset table lists the ASUS DRW-24B1ST at +6 samples across 742
submissions, and the AOPEN DVD RW ISU8424E at +1292 samples. The extremes
in that table run from -1164 to +1776 samples.

## Reed-Solomon

The error-correction math CTDB uses to rebuild damaged audio from
[parity](#parity). It is the same family of math that lets a CD player
play through a small scratch, and that recovers data from a damaged QR
code. Reed-Solomon can reconstruct a limited number of damaged pieces
exactly; past that limit it reports that it cannot, rather than guessing.

## TOC id (table of contents id)

A fingerprint of a disc's track layout, meaning how many tracks it has
and where each one starts and ends. Two pressings with identical track
layouts share a TOC id; a reissue with a bonus track does not.

Both databases find a disc by its track layout, and each derives its own
identifier from it. The id shown in the corner of a disc card is CTDB's.
