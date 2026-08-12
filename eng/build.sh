#!/usr/bin/env bash
# Single source of truth for building CUETools Linux locally and in CI.
#
# Stages the fork submodule's vendor sources, then builds and tests. The
# fork's committed NuGet lock files are Windows/SDK-8 specific today (fork
# issue #7), and NuGet refuses to restore with lock machinery disabled
# while a lock file exists on disk (NU1005), so the fork's lock files are
# set aside for the build and their committed bytes restored afterward,
# even on failure. Determinism comes from exact version pins plus the
# submodule commit pin (decision D-034).
set -euo pipefail
cd "$(dirname "$0")/.."

SUB=extern/cuetools_2026
LOCKFLAGS=(-p:RestorePackagesWithLockFile=false -p:RestoreLockedMode=false)
CONFIG="${CONFIG:-Release}"

mapfile -t LOCKFILES < <(git -C "$SUB" ls-files '*packages.lock.json')
restore_locks() {
  if [ "${#LOCKFILES[@]}" -gt 0 ]; then
    git -C "$SUB" checkout --quiet -- "${LOCKFILES[@]}"
  fi
}
trap restore_locks EXIT
for f in "${LOCKFILES[@]}"; do rm -f "$SUB/$f"; done

pwsh -NoProfile -File "$SUB/eng/ci/Prepare-VendorSources.ps1" \
  -RepositoryRoot "$PWD/$SUB"

dotnet build src/CUETools.Linux.App/CUETools.Linux.App.csproj \
  -c "$CONFIG" --nologo "${LOCKFLAGS[@]}"
dotnet build tests/CUETools.Linux.Tests/CUETools.Linux.Tests.csproj \
  -c "$CONFIG" --nologo "${LOCKFLAGS[@]}"
dotnet test tests/CUETools.Linux.Tests/CUETools.Linux.Tests.csproj \
  -c "$CONFIG" --no-build --nologo

restore_locks
trap - EXIT
if [ -n "$(git -C "$SUB" status --porcelain)" ]; then
  echo "ERROR: fork submodule working tree is dirty after build:" >&2
  git -C "$SUB" status --porcelain >&2
  exit 1
fi
echo "build.sh: staging, build, tests, and submodule cleanliness all green."
