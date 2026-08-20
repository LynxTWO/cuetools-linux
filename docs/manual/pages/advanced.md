# The Advanced page, engine options

The **Advanced** page holds the engine options the original CUETools
exposed through a property grid. Most people never need them; they are
here so that the ones you do need have a switch instead of a text edit.
Changes apply immediately and are saved when the app exits, except the
proxy password, which is covered below. Nothing on this page touches
your music.

The page sits at the bottom of the rail, under **Settings**.

## Output extras

- **Create TOC files** also writes a `.toc` file (each track's start,
  length, and sector range) next to the rip.
- **Detailed CTDB log** includes full per-track CTDB detail in the
  separate AccurateRip/CTDB report. The EAC-style rip log is unchanged.
- **Write CTDB tags on encode** and **on verify** embed CTDB confidence
  and repair info into the encoded files' tags.
- **Use ID3v2.4 instead of ID3v2.3** switches MP3 tag versions. v2.3 is
  the widely compatible default; turn this on only if your player
  supports v2.4.
- **Write CDTOC tag** embeds the disc's table of contents in the tags,
  so a file can be matched back to a specific pressing later.

## Cover art and metadata

- **Cover art file names** is the semicolon-separated list of filenames
  the folder scan accepts as the album cover; `%album%` is replaced with
  the album title. **Search subfolders** widens that scan.
- **Album art search** and **Metadata search** set how hard CTDB (the
  CUETools Database) is asked for images and for album data: from None
  (never ask) to Extensive.
- **Cache metadata** keeps a local copy of fetched metadata so repeat
  lookups skip the network.
- **CTDB server** is the database hostname, `db.cuetools.net` unless
  you run your own.

## Network

**Proxy mode** decides how lookups reach the network: None (direct),
System (the system-configured proxy), or Custom, which uses the server,
port, and user rows under it.

The **Proxy password** row never displays a saved value; it shows
`Credential set` or `No credential`, and takes a replacement or an
explicit **Clear**. On Linux the password is held in memory only: this
build has no protected credential storage, and it does not write
secrets to the settings file as plain text instead. A password you set
works until the app closes, and the next launch starts at
`No credential` again. The same rule is described in
[settings](settings.md#passwords-and-api-keys-are-not-stored).

The three **Freedb** rows are the identity and server for legacy
FreeDB/gnudb lookups. Most servers accept any email value.

## Related topics

- [Settings and where files live](settings.md)
- [Terms used in this manual](glossary.md)
