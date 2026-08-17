#!/usr/bin/env bash
# Assert that a staged package tree carries every native codec and CLI encoder
# its manifests name, with matching SHA-256.
#
# This exists because both packaging scripts used to collect only top-level
# *.so files while the build publishes codecs into native/ and encoders/. The
# result passed every gate: the package built, installed, launched, and ripped
# FLAC (which the managed encoder provides), while WavPack, Monkey's Audio,
# MP3, Opus, Vorbis, and Musepack silently reported unavailable. Presence alone
# is not enough either - a truncated or stale copy is worse than a missing one,
# because the loader's hash check would reject it at runtime instead.
#
# Usage: assert-payload.sh <staged-app-directory>
set -euo pipefail
ROOT="${1:?usage: assert-payload.sh <staged-app-directory>}"
cd "$(dirname "$0")/.."

fail() { echo "ERROR: payload assertion: $*" >&2; exit 1; }

# The manifests in obj/ are the source of truth: they are what the build
# produced and what the app validates against at load time.
NATIVE_MANIFEST=obj/native/native-codecs.json
ENCODER_MANIFEST=obj/encoders/linux-encoders.json

checked=0

check_one() {
  local file="$1" want="$2" dir="$3"
  local path="$ROOT/$dir/$file"
  [ -f "$path" ] || fail "$dir/$file is missing from the staged package"
  local got
  got=$(sha256sum "$path" | cut -d' ' -f1)
  [ "$got" = "$want" ] ||
    fail "$dir/$file hash mismatch (manifest $want, staged $got)"
  checked=$((checked + 1))
}

if [ -f "$NATIVE_MANIFEST" ]; then
  [ -f "$ROOT/native/native-codecs.json" ] ||
    fail "native/native-codecs.json is missing; the loader cannot validate anything without it"
  while IFS=$'\t' read -r file sha; do
    [ -n "$file" ] || continue
    check_one "$file" "$sha" native
  done < <(python3 -c '
import json, sys
m = json.load(open(sys.argv[1]))
for lib in m.get("libraries", []):
    print(lib["file"] + "\t" + lib["sha256"])
' "$NATIVE_MANIFEST")
fi

if [ -f "$ENCODER_MANIFEST" ]; then
  [ -f "$ROOT/encoders/linux-encoders.json" ] ||
    fail "encoders/linux-encoders.json is missing; the catalog cannot hash-check a packaged encoder without it"
  while IFS=$'\t' read -r file sha; do
    [ -n "$file" ] || continue
    check_one "$file" "$sha" encoders
    [ -x "$ROOT/encoders/$file" ] ||
      fail "encoders/$file is not executable in the staged package"
  done < <(python3 -c '
import json, sys
m = json.load(open(sys.argv[1]))
for enc in m.get("encoders", []):
    name = enc.get("file") or enc.get("name")
    sha = enc.get("sha256")
    if name and sha:
        print(name + "\t" + sha)
' "$ENCODER_MANIFEST")
fi

[ "$checked" -gt 0 ] ||
  fail "no payload entries were checked; both manifests were absent, so this package ships managed-only codecs"

echo "payload assertion: $checked manifest entries present with matching hashes"
