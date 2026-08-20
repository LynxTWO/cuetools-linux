# How a CD Works, the explorable disc

**How a CD Works**, under LEARN in the rail, is a lesson you can move
through: a to-scale disc you pan and zoom, next to a short illustrated
course on what's physically on the disc. It's not connected to a live
rip (the Rip page has its own live disc), and nothing here reads your
drive or touches any file. This page is purely for the pleasure of
knowing what you're holding.

## The stage

The left side is the disc, drawn at true scale. Drag to move, scroll
to zoom. And keep zooming: the whole trick of the page is that the
zoom runs continuously from the full 120 mm disc all the way down to
the data track itself, and a scale bar in the corner always shows a
round-number ruler (from `50 mm` down to fractions of a micrometre) so
you know how deep you are.

What you see on the way down is the real geometry of the format:

- At arm's length: the center hole, the clamping ring, the mirror
  band, and the broad data band that fills most of the disc. Familiar
  territory - you've held this a thousand times.
- Around the point where the scale bar reads tens of micrometres,
  something appears out of the blank metal: the spiral. Parallel turns
  wound 1.6 um apart, which is why the disc you've been squinting at
  your whole life just looks smooth. They draw as stripes because at
  this magnification a turn of the spiral is very nearly straight.
- Closer still, each turn breaks into pits and lands: dashes of 0.83
  to 3.05 um (the format's legal pit lengths), about half a micrometre
  wide, sitting on the continuous track line. That dash pattern is the
  music. All of it - every album you own on CD - is a five-kilometre
  line of bumps too small to see.

One honest note: the pit pattern is illustrative. The lengths and the
track geometry are the real CD specification, but the particular
sequence is generated, not read from any actual disc. The stage
teaches the geometry, not the content.

## The lesson panel

The right side explains what you're looking at. The spiral: one
continuous track about 5 km long, read from the inside out, not
concentric rings. Pits and lands: it's the transitions that carry the
bits - a change is a 1, no change is a 0. Constant data rate: the disc
spins faster at the hub and slower at the rim so the track passes the
laser at a constant speed, which is exactly why a rip's read position
races through the early tracks and settles down toward the rim. You've
watched that happen on the Rip page; this is the reason.

Under those sits a cross-section of the layers, and it carries the
lesson that matters most for your discs: the laser focuses up through
1.2 mm of clear polycarbonate onto the aluminium data layer just under
the label. The two sides of a disc fail completely differently because
of it. A scratch on the clear side is out of focus to the laser and
often recoverable; damage on the label side goes straight into the
data layer itself, and that data is simply gone. Handle the shiny side
with confidence and the label side with respect. That asymmetry is the
"why" behind [repair](repair.md) and behind every careful-handling
habit worth having.

## Related topics

- [Ripping a CD](rip.md)
- [Verify & Repair](repair.md)
