# The Drive & Read page, what the hardware reports

The **Drive & Read** page is a live readout of the selected optical
drive: its identity, what it can read and write, its speeds, its
AccurateRip read offset, and the calibration CUETools has measured for
it. Everything is read from the drive itself over SCSI, nothing is
hardcoded, and the page only reads: neither button writes anything to
a disc.

Hover any label on the page for a plain-English explanation of what
that value means; the page doubles as a tour of what an optical drive
actually tells the computer.

## Detect and Calibrate

**Detect** re-reads the selected drive's identity, capabilities,
speeds, and offset. It needs no disc, and it runs by itself when the
page opens or the selected drive changes.

**Calibrate** probes how the drive actually behaves with a disc
loaded: whether it serves repeated reads from its cache (and what
flush size defeats that), whether it will read into the lead-in and
lead-out, and its real speed range. It reads audio sectors and writes
nothing. You rarely need the button, because calibration runs
automatically before a drive's first Rip, Verify, or Test & Copy; it
is here for refreshing the record by hand. With no disc loaded, the
status line asks for one: "Calibration needs an audio disc in the
drive. Insert one and try again."

The **DRIVE** picker stays synchronized with the Rip page's selection,
and it locks while a job owns the drive; the status line says the
details will refresh when the job finishes.

## The identity card

The top card is the drive's own answer to the SCSI INQUIRY command:
maker and model, firmware version, and revision string, with the media
type currently in the tray on the first line. The **AccurateRip name**
is the exact key used to look the drive up in the shared offset table.

**READ OFFSET** is the drive's fixed
[read offset](glossary.md#read-offset) from the AccurateRip table, in
samples. Every drive reads a few samples early or late compared to a
reference drive; the ripper shifts every read by this amount so your
rip lines up bit-for-bit with everyone else's. `--` means the drive is
not in the offset table. All three house drives measure +6 samples.

## The capability lamps

Six cards summarize what the drive reported through GET CONFIGURATION.
A green lamp means the capability is present:

- **Reads** and **Writes** list the disc families (CD, DVD, Blu-ray).
- **C2 error pointers**: whether the drive can flag suspect samples
  per sector. An amber lamp means it cannot, so a secure rip re-reads
  more instead.
- **CD-Text**: whether the drive can read titles a disc carries in its
  lead-in.
- **Max read speed** and **Max transfer** are the advertised ceiling
  and the largest single SCSI transfer the driver will move.

## The calibration card

**CALIBRATION** shows what CUETools measured for this drive and saved
in its [drive calibration](glossary.md#drive-calibration) file: the
cache behaviour and measured flush size, whether the lead-in and
lead-out edges accepted real reads, and the probed speed range, with
the date it was measured. `not calibrated` means the drive has not yet
run its first job on this machine.

## Supported media and drive features

**SUPPORTED MEDIA** lists every disc profile the drive says it
handles (CD-R, DVD+RW, and so on). **DRIVE FEATURES** is the complete
GET CONFIGURATION feature list: a lit dot means the feature is active
right now for the loaded media, an unlit dot means it is supported but
not currently in use, and the hover text carries each feature's raw
code.

## Related topics

- [Ripping a CD, and the read modes](rip.md)
- [Settings and where files live](settings.md)
- [Terms used in this manual](glossary.md)
