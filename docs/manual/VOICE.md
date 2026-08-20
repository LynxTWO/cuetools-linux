# Manual voice

House rules for `docs/manual/pages/`, the human manual. Collected while
dialing in the Verify golden page with the owner on 2026-08-15, and
confirmed by the owner the same day. These override the
writing-user-manuals skill defaults where they differ.

The fact rules do not move: every example, cause, and number needs a
receipt, and a conflicting claim goes to `needs-verification.md`, not
into print.

## Sentence mechanics

- Asides go in parentheses, not paired " - " dashes: "anything ambiguous
  (two manifests over the same audio, discs that cannot be ordered) is
  rejected with the reason." Em dashes stay banned.
- Add a comma where a reader would take a breath reading the sentence
  aloud. Read each sentence in head-voice; if you would stumble reading
  it to someone at the bench, recast it.
- Contractions are fine.
- "Simply", "obviously", and "just" stay out of steps the reader
  performs.
- Prefer the plain verb to the flourish. "with the databases' current
  answers" beats "with that day's answers".

## Naming and terms

- Name the databases at first mention in a section: "the databases
  (AccurateRip and CTDB)", not just "the databases".
- Terms a normal reader will not know get a `pages/glossary.md` entry,
  and the first use on a page links to it. The site generator turns any
  `glossary.md#term` link into a hover definition, pulled from the
  entry's first paragraph, so write that paragraph as a standalone
  definition. The sentence must still make sense without following the
  link.
- Keep the reference-link pattern: "see offline behavior and backfill."

## Explanations

- Explanations carry one or two real examples with receipts. Verified
  2026-08-15 for drive read offsets (AccurateRip drive offset table):
  ASUS DRW-24B1ST reads +6 samples (742 submissions, 100% agreement);
  AOPEN DVD RW ISU8424E reads +1292 samples; table extremes are +1776
  (ASUS CD-S480-A5) and -1164 (COMPAQ CD-ROM LTN403). The house matrix
  drives (PLDS DU8A5SH, ASUS BW-16D1HT, HL-DT-ST WH16NS40) all measured
  +6 during SLICE-009 calibration.
- When a message or verdict is explained, say what to do AND why it
  happens. An unverified cause goes to `needs-verification.md` first,
  and error-message examples are harvested from the real strings in the
  code, never invented.
- Close a good outcome plainly and warmly: "Nothing; the rip matches
  other people's rips, so you're all set." Warmth must state something
  true. The app is never praised.
- Warmth is where false claims sneak in. Verified 2026-08-15: nothing in
  either modern head calls the CTDB client's `Submit`, so verifying an
  unknown pressing contributes nothing to any database, and no page may
  imply otherwise. SLICE-012 would change that; the `not in database`
  row and `notes/install.md` both get updated in the same batch if it
  ships.
- A hand-written playlist is a valid manifest: playlist parsing skips
  blank lines and lines starting with "#" (`CUESheet.cs`), so a plain
  text `.m3u` listing the audio files in album order, one per line,
  works. Troubleshooting may tell the reader to write one.

## Warmth (D-077, 2026-08-20)

The house adaptation of the owner's Internet Human Mode skill. Its core
rule survives intact: change the surface, never the thought. Its upper
levels do not: nothing here manufactures typos, dropped apostrophes, or
rushed-typing texture to look human. Humanity comes from voice, not
damage.

- Write like a person talking to a person: contractions, direct "you",
  varied sentence length, plain verbs. If a sentence would sound stiff
  read aloud at the bench, recast it.
- Manual pages and the glossary sit at level 1: warm precision. Every
  fact rule in this file still binds; warmth never spends a receipt.
- How a CD Works sits at level 2-3: a lesson may carry personality
  (wonder, a wry aside) that a reference page should not. The numbers
  stay exact.
- Humor must be true. A joke that bends a fact is a fact error with a
  smile on it.
- Emoji and comic archetypes stay out, same as every other typographic
  flourish. The no-Unicode rule already covers them; this line makes it
  explicit.

## UI text

- UI strings are quoted verbatim in the manual, even when a nicer
  wording is planned. Changing a string is app work, filed separately;
  the manual updates when the app does. The one change made this way so
  far: "instead of guessing" became "so CUETools does not have to
  guess" (fork branch `fix/discovery-message-wording`).

## Page layout

- Task pages follow the structure in the writing-user-manuals skill:
  effects on the reader's files in the opening paragraph, a
  before-you-start table, numbered steps, one verdict row per distinct
  message, troubleshooting headed by an observable symptom, and the
  architecture parked in "How it works".
- Screenshots are figures with real alt text and a caption. The
  generator turns `![alt](name.png)` plus a following italic line into
  `<figure>` and `<figcaption>`.
