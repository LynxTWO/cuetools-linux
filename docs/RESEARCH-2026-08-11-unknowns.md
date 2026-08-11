# Research: closing U-003 and U-004 (2026-08-11)

Companion to RESEARCH-2026-08-11.md. Evidence gathered from the
UnknownException/cuetools.net fork (branch cueripper-avalonia @ 50066d8) and
Avalonia official sources.

## U-003 CLOSED: Linux drive access in the UnknownException fork

**Answer: direct P/Invoke of libc open/close/ioctl with SG_IO on /dev/sr*.
No libcdio, no cdparanoia.** A Linux shim slotted underneath the existing
Bwg.Scsi Windows IOCTL layer, not a rewrite.

Key facts (files (c) 2025 Max Visser, GPL-2.0):

- `Bwg.Scsi/ISysDev.cs`: OS-device interface (Open, Control, LastError).
- `Bwg.Scsi/LinDev.cs`: Linux implementation, netstandard2.0-only build.
  Emulates Windows IOCTL codes: SCSI_PASS_THROUGH_DIRECT -> fills a
  SG_IO_HDR from the Windows struct and calls ioctl(fd, SG_IO);
  GET_CAPABILITIES -> SG_GET_RESERVED_SIZE; MEDIA_REMOVAL -> CDROM_LOCKDOOR.
  dxfer_direction hardcoded device-to-host (no data-out commands). Header
  warns "highly experimental".
- `Bwg.Scsi/Device.cs` (~line 950): runtime backend pick via
  RuntimeInformation.IsOSPlatform. All ~3,700 lines of MMC command building
  (READ CD 0xBE, C2 modes, subchannel) are shared and unchanged.
- `CUETools.Interop/Linux.cs`: DllImport("libc") for open/close/ioctl/lstat/
  dlopen/dlsym/dlclose; SG_IO = 0x2285; 64-bit only.
- Enumeration: `CDDrivesList.DrivesAvailable()` lstats /dev/sr0..sr9 (a
  self-described "quirky workaround"). Disc detection: 500 ms polling thread
  with ioctl(CDROM_DRIVE_STATUS) == CDS_DISC_OK.
- No cache-defeat/FUA code; no cdrom-group permissions story; EACCES just
  logs a warning.
- Codec natives: publish_linux64.sh builds libFLAC and LAME 3.100 from
  source, renames to the Windows DLL names (libFLAC_dynamic.so), and a
  DllImportResolver hook (`CUERipper.Avalonia/Utilities/LibraryResolver.cs`)
  maps extension-less Windows import names to plugins/x64/<name>.so.

**Takeaway for CUETools Linux:** the whole Bwg.Scsi/SCSIDrive MMC stack
ports with a ~370-line shim plus a DllImportResolver. But the fork's
enumeration (sr0-9 poll), permissions story, hotplug handling, and data-out
support are acknowledged stopgaps, and it carries none of the 2026 fork's
calibration/cache-defeat assurance. Our drive-access module does the same
SG_IO core properly: udev-informed enumeration, cdrom-group guidance,
data-out support, and the calibration invariants carried from the fork.

## U-004 CLOSED: Avalonia Linux display-server status (August 2026)

- Avalonia 11.3 and 12.0: X11 only on desktop Linux (XWayland on Wayland
  sessions).
- Avalonia 12.1 (12.1.0 Jul 9, 2026; 12.1.1 Jul 29, 2026) ships the first
  native Wayland backend, experimental and opt-in: reference the
  `Avalonia.Wayland` package (MIT) and call .UseWayland(); not picked up by
  UsePlatformDetect() yet. Default on Wayland sessions remains XWayland.
- Avalonia 12 requires .NET 8+; .NET 10 recommended (required only for
  mobile). 11.3.x is the maintenance line for netstandard2.0/.NET Framework
  consumers; no public LTS designation confirmed.
- NativeAOT: supported; compiled bindings required. Avalonia 12 turns
  compiled bindings on by default (the biggest AOT foot-gun removed) and
  quantifies 1,960 ms -> 460 ms AOT startup (Android measurement).
- Avalonia 12 is the first .NET UI framework with a native Linux
  accessibility backend (AT-SPI2), which serves the protected Accessible
  goal.
- FluentAvaloniaUI 3.0.x (Jun-Jul 2026) supports Avalonia 12.

Full source list in the interview session record; primary: avaloniaui.net
blog posts "Avalonia 12" (2026-04-07), "What's new in Avalonia 12.1"
(2026-07-08), "Bringing Wayland Support to Avalonia" (2025-09-19), docs
"Native AOT" (2026-05-07), "Breaking changes in Avalonia 12", NuGet pages
for Avalonia, Avalonia.Wayland, FluentAvaloniaUI; UnknownException fork raw
sources @ 50066d8.
