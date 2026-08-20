# How a CD Works, the explorable disc

**How a CD Works**, under LEARN in the rail, is a lesson you can move
through: a to-scale disc you pan and zoom, next to a short illustrated
course on what is physically on the disc. It is not connected to a
live rip (the Rip page has its own live disc); nothing here reads your
drive or touches any file.

## The stage

The left side is the disc, drawn at true scale. Drag to move, scroll
to zoom. The zoom runs continuously from the whole 120 mm disc down to
the data track itself, and a scale bar in the corner always shows a
round-number ruler (from `50 mm` down to fractions of a micrometre) so
you know how far in you are.

What you see on the way down is the real geometry of the format:

- At arm's length: the center hole, the clamping ring, the mirror
  band, and the broad data band that fills most of the disc.
- Around the point where the scale bar reads tens of micrometres, the
  data band resolves into the spiral: parallel turns wound 1.6 um
  apart. They look like stripes because at this magnification a turn
  of the spiral is very nearly straight.
- Closer still, each turn breaks into pits and lands: dashes of 0.83
  to 3.05 um (the format's legal pit lengths), about half a micrometre
  wide, sitting on the continuous track line.

The pit pattern is illustrative: the lengths and track geometry are
the real CD specification, but the particular sequence is generated,
not read from any actual disc. The stage teaches the geometry, not
the content.

## The lesson panel

The right side explains what you are looking at: the spiral (one
continuous ~5 km track read from the inside out), pits and lands (how
transitions carry the bits), constant data rate (why a disc spins
faster at the hub, and why a rip speeds up toward the rim), and a
cross-section of the layers. The cross-section shows the laser
focusing up through 1.2 mm of polycarbonate onto the aluminium data
layer, which is why the two sides of a disc fail so differently: a
scratch on the clear side is out of focus to the laser and often
recoverable, while damage on the label side destroys the data layer
itself. That asymmetry is the "why" behind
[repair](repair.md) and careful disc handling.

## Related topics

- [Ripping a CD](rip.md)
- [Verify & Repair](repair.md)
