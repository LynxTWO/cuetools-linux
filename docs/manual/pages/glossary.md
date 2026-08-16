# Glossary

Terms used across this manual. Each entry explains the idea first, then
the detail you need if you want it.

## AccurateRip

A public database of CD rip checksums, submitted by people using ripping
software over many years. It stores per-track checksums for a disc, keyed
by that disc's track layout. When your checksums match theirs, independent
copies of the same disc agree with yours.

## confidence

A count of how many independent submissions agree with your audio.
Higher numbers mean more independent agreement. Confidence is not a
percentage or a score, and a low number on a rare disc is normal.

The album-level AccurateRip figure is the lowest confidence of any track
on the disc, so `accurate | confidence 4` means every track agrees with
at least four other rips. The CTDB figure works differently: it is the
combined confidence of every database entry that matches you, so it is
usually a much larger number.

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

## independently verified

A repaired copy is checked again from scratch, as if you had loaded it
yourself, before CUETools keeps it. The repair math finishing is not
treated as success: after repair writes the new files, CUETools runs a
fresh verification on them and asks both databases about the result. The
repaired copy is published only when AccurateRip or CTDB confirms it.

## parity

Extra data stored alongside a recording that can rebuild missing or
damaged parts of it. Parity has a limit: it can rebuild damage up to a
certain amount and no further, so a badly damaged disc can be beyond
repair even when the database knows it.

CTDB holds parity for many of the discs it knows, but not all of them.
An entry without parity can still confirm a clean rip; it just cannot
repair a damaged one.

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
