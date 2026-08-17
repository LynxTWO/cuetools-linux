# cuetools-linux working notes

Native Linux port of CUETools 2026 (Avalonia + .NET 10). The engine and the
shared app core are consumed from the fork LynxTWO/cuetools_2026, vendored here
as the `extern/cuetools_2026` submodule.

## Two repositories are always in play

Almost every change touches both: the shared code lives in the fork, the head
and its tests live here. That has bitten this project repeatedly, so:

- **Never chain `cd` with a commit.** A command like
  `cd ../cuetools-linux && ... && git add -A && git commit` commits in whichever
  repository the `cd` landed in, not the one the edits were made in. This has
  produced fork commit messages on Linux commits four separate times, once
  amending an already-pushed commit. Issue repository-changing git commands as
  their own call, and confirm `git branch --show-current` first.
- **Never `git add -A` here.** The owner keeps uncommitted work in this tree
  (packaging scripts, `README.md`, `docs/manual/notes/install.md`,
  `docs/manual/notes/settings.md`, `src/.../RipView.axaml`). Stage explicit
  paths. If something of theirs is caught anyway, `git reset --soft` and
  `git restore --staged <their paths>` before recommitting.
- A fork change reaches this repo only through a submodule pin bump, which is a
  D-028 owner-approval item.

## Dev loop for a fork change

Iterate without pushing the fork:

```console
git -C extern/cuetools_2026 fetch /home/daniel-boyd/DEV/apps/cuetools_2026 <branch>
git -C extern/cuetools_2026 checkout FETCH_HEAD
./eng/build.sh --managed-only
```

Restore the pin with `git -C extern/cuetools_2026 checkout <merged sha>` once
the fork branch merges.

When tests here exercise fork code from **two** open branches, no single pin
satisfies both and one set fails for a reason that looks like a regression but
is not. Merge the fork branches into a scratch branch, point the submodule at
that, and run the suite once against the tree master will actually have.

## Build and test

- Always build through `./eng/build.sh`. It sets the fork's Windows-specific
  NuGet lock files aside and restores their committed bytes afterwards; a bare
  `dotnet build` against the submodule rewrites them (NU1004 on the next CI
  run). `--managed-only` skips the vendored native codec and CLI encoder builds.
- `dotnet` run from inside the fork tree picks up its `global.json` SDK pin,
  which is not installed here. Run from another directory if you must call it
  directly.
- The fork's `CUETools.Wpf.Tests` cannot run on Linux. A test that needs to run
  locally belongs in `tests/CUETools.Linux.Tests`, which drives the shared view
  models directly. Copy the fakes from `QueueFlowTests` rather than inventing
  interface shapes.
- Tests that drive a view model must be `[AvaloniaFact]`, not `[Fact]`:
  RelayCommands register on the static `RequeryHub` for the process lifetime, so
  a plain xunit worker thread broadcasts cross-thread into controls left by
  earlier headless tests.
- A test run started in the repo root drops app state here (`CUETools2026/`,
  `cuetools-linux/`, `CUE Tools/`). Those paths are gitignored; do not commit
  them.

## Documentation

- `docs/manual/pages/` is the user manual, `docs/manual/notes/` is the
  engineering sourcebook, `docs/manual/VOICE.md` is the confirmed wording
  standard, and `docs/manual/needs-verification.md` holds claims that must not
  be printed until checked. See the `writing-user-manuals` skill for the method.
- Rebuild the site with `python3 eng/build-manual.py` and check for dead links,
  dead anchors, missing images, and leftover markdown before committing.
- Product defects found while documenting go to
  `docs/FINDINGS-2026-08-16-manual-pass.md`, not into the manual.
