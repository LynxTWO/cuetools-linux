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

# --managed-only: skip the vendored native codec and CLI encoder builds and
# run only the tests that need neither. This is the macOS drift lane's mode
# (docs/MACOS-ROADMAP.md): those scripts and their hash manifests are
# .so/ELF-specific, and the app degrades honestly without the artifacts
# (csproj payloads are Exists-conditioned; both loaders skip on a missing
# manifest). Everything else still runs: vendor staging, app + test build,
# managed suite, submodule cleanliness.
MANAGED_ONLY=0
PUBLISH=0
for arg in "$@"; do
  case "$arg" in
    --managed-only) MANAGED_ONLY=1 ;;
    --publish) PUBLISH=1 ;;
    *) echo "unknown argument: $arg" >&2; exit 2 ;;
  esac
done

# No mapfile and no bare empty-array expansion here: macOS ships bash 3.2
# and the macos CI lane runs this script with it (mapfile arrived in bash
# 4; empty "${arr[@]}" trips set -u on 3.2).
LOCKFILES=()
while IFS= read -r f; do LOCKFILES+=("$f"); done \
  < <(git -C "$SUB" ls-files '*packages.lock.json')
restore_locks() {
  if [ "${#LOCKFILES[@]}" -gt 0 ]; then
    git -C "$SUB" checkout --quiet -- ${LOCKFILES[@]+"${LOCKFILES[@]}"}
  fi
}
trap restore_locks EXIT
for f in ${LOCKFILES[@]+"${LOCKFILES[@]}"}; do rm -f "$SUB/$f"; done

pwsh -NoProfile -File "$SUB/eng/ci/Prepare-VendorSources.ps1" \
  -RepositoryRoot "$PWD/$SUB"

if [ "$MANAGED_ONLY" = 0 ]; then
  # Vendored native codecs (D-042): build once, then incrementally via cmake.
  # The app layout and tests consume obj/native through the csproj payload.
  ./eng/build-native-codecs.sh

  # Curated CLI encoders (D-047): pinned-source ELF builds + hash manifest.
  ./eng/build-cli-encoders.sh
fi

TESTARGS=()
if [ "$MANAGED_ONLY" = 1 ]; then
  TESTARGS+=(--filter
    "FullyQualifiedName!~NativeCodecTests&FullyQualifiedName!~CliEncoderTests")
fi

dotnet build src/CUETools.Linux.App/CUETools.Linux.App.csproj \
  -c "$CONFIG" --nologo "${LOCKFLAGS[@]}"
dotnet build tests/CUETools.Linux.Tests/CUETools.Linux.Tests.csproj \
  -c "$CONFIG" --nologo "${LOCKFLAGS[@]}"
dotnet test tests/CUETools.Linux.Tests/CUETools.Linux.Tests.csproj \
  -c "$CONFIG" --no-build --nologo ${TESTARGS[@]+"${TESTARGS[@]}"}

# --publish: NativeAOT publish inside the same lock set-aside window
# (publish restores too, so it needs the identical treatment; D-034).
if [ "$PUBLISH" = 1 ]; then
  dotnet publish src/CUETools.Linux.App/CUETools.Linux.App.csproj \
    -c "$CONFIG" -r linux-x64 --nologo "${LOCKFLAGS[@]}"
fi

restore_locks
trap - EXIT
if [ -n "$(git -C "$SUB" status --porcelain)" ]; then
  echo "ERROR: fork submodule working tree is dirty after build:" >&2
  git -C "$SUB" status --porcelain >&2
  exit 1
fi
echo "build.sh: staging, build, tests, and submodule cleanliness all green."
