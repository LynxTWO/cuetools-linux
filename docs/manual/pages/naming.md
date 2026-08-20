# The Naming page, your file and folder scheme

The **Naming** page edits how rips and conversions name their folders
and files: one template, five clean-up rules, and a live preview that
renders real examples through the same engine the Rip, Convert, and
Queue pages use. Editing here changes future output only; nothing on
this page renames files you already have.

Changes apply as you type and are saved when the app exits, like every
other setting. The keys behind the page are the `WpfNaming...` family
documented in [settings](settings.md#how-rips-and-conversions-are-named).

## The template

The **TEMPLATE** box holds the scheme. The `/` divides the folder part
from the file part, and square brackets mark a piece that disappears
when its field is empty. The shipped template is:

```text
%albumartist% - %album%[%releasedescriptor%]/[%disc%]%tracknumber% - %title%[%featsuffix%]
```

Under **FIELDS**, every placeholder the engine understands is a button.
Click one and it drops in at your cursor, so you can build a scheme
without memorizing the field names.

**PRESET** offers three starting points: **Archival (default)**, the
shipped scheme; **Artist - Album (year)**, a flat
artist-album-year folder; and **Simple**, plain artist/album folders
with the featured-artist move and the release descriptor turned off.
Picking a preset replaces the whole scheme, template and switches
together, and the preview updates at once.

## The clean-up rules

The five switches are the same rules the
[settings reference](settings.md#how-rips-and-conversions-are-named)
documents key by key: featured-artist extraction, artist separator
unification, leading-article handling ("The Beatles" filed as
"Beatles, The"), awkward-character stripping, and the bracketed release
descriptor on the album folder. Each switch's hover text on the page
says what it does; the settings page's table records the exact
characters and words each rule rewrites.

## The live preview

**LIVE PREVIEW** renders canned example albums (a single-artist album,
a guest credit, a multi-disc live set, a various-artists soundtrack)
through the exact engine the rip and convert paths use, so what the
preview shows is what the output will be named. When a disc is loaded
on the Rip page, a group headed **Disc in tray:** leads the preview,
rendering your actual disc's first tracks through the current scheme.

One near-miss to avoid: the Settings page's **Track filename template**
row is a different template. It belongs to the engine and names the
tracks of a conversion written in tracks mode; rips are named by this
page's scheme, whatever that row says.

## Related topics

- [Settings and where files live](settings.md)
- [Ripping a CD](rip.md)
- [Converting existing rips](convert.md)
