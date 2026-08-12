# User manual sourcebook

The full HTML user manual is assembled AFTER the port surface stabilizes
(owner decision D-038). Until then, this directory banks the raw material
while each capability ships, when the knowledge is fresh and the evidence
exists:

- `notes/<topic>.md`: plain-English capture of what the user can do, the
  exact UI vocabulary, and what every verdict phrase means. One claim per
  paragraph; never invent behavior - if it is not verified in the app, it
  does not go in a note.
- Screenshots live in `../evidence/` and are referenced by filename; they
  are the manual's future illustrations.
- Per-release definition of done includes keeping these notes current for
  user-visible changes (EDD section 17).

Assembly plan (later): a beautiful static HTML manual, structured by user
task (install, verify, repair, convert, rip), themed to the 2026 identity,
built from these notes plus the evidence screenshots.
