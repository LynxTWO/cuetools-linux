#!/usr/bin/env bash
# Build the vendored native codec libraries from the fork's pinned sources
# (D-042: vendored pinned natives, never distro packages).
#
# Inputs (all pinned):
#   - libFLAC, WavPack: the staged, patched vendor trees under
#     extern/cuetools_2026/obj/vendor-sources/current (run eng/build.sh or
#     the fork's Prepare-VendorSources.ps1 first; staging hash-validates).
#   - Monkey's Audio: the pinned MAC_1320_SDK.zip expanded into build
#     scratch, with the fork's hash-pinned MACLibDll wrapper override
#     applied over it and EXCLUDE_CIO defined, exactly like the Windows
#     build (eng/ci/Build-NativeDependencies.ps1).
#
# Outputs: obj/native/<name>.so named by each wrapper's DllName, plus
# obj/native/native-codecs.json recording the SHA-256 of every library.
# The app validates these hashes before registering any path; packaging
# ships both the libraries and the manifest.

set -euo pipefail
cd "$(dirname "$0")/.."

FORK=extern/cuetools_2026
STAGED=$FORK/obj/vendor-sources/current/ThirdParty
BUILD=obj/native-build
OUT=obj/native
JOBS=$(nproc)

[ -d "$STAGED/flac" ] || { echo "ERROR: vendor staging missing; run eng/build.sh first" >&2; exit 1; }

mkdir -p "$OUT"

echo "== libFLAC (staged, patched vendor tree)"
cmake -S "$STAGED/flac" -B "$BUILD/flac" -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON -DWITH_OGG=OFF -DBUILD_PROGRAMS=OFF \
  -DBUILD_EXAMPLES=OFF -DBUILD_TESTING=OFF -DBUILD_DOCS=OFF \
  -DBUILD_CXXLIBS=OFF -DINSTALL_MANPAGES=OFF >/dev/null
cmake --build "$BUILD/flac" -j"$JOBS" >/dev/null
cp "$BUILD/flac/src/libFLAC/libFLAC.so."*.*.* "$OUT/libFLAC_dynamic.so"

echo "== WavPack (staged, patched vendor tree)"
cmake -S "$STAGED/WavPack" -B "$BUILD/wavpack" -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED_LIBS=ON -DWAVPACK_BUILD_PROGRAMS=OFF \
  -DWAVPACK_ENABLE_ASM=ON -DWAVPACK_BUILD_DOCS=OFF >/dev/null
cmake --build "$BUILD/wavpack" -j"$JOBS" >/dev/null
cp "$(readlink -f "$BUILD/wavpack/libwavpack.so")" "$OUT/wavpackdll.so"

echo "== Monkey's Audio (pinned SDK archive + pinned wrapper override, EXCLUDE_CIO)"
MACSRC=$BUILD/mac-src
rm -rf "$MACSRC"
mkdir -p "$MACSRC"
unzip -q "$FORK/ThirdParty/MAC_SDK/MAC_1320_SDK.zip" -d "$MACSRC"
mkdir -p "$MACSRC/Source/MACLibDll"
cp "$FORK/ThirdParty/MAC_SDK/Source/MACLibDll/MACLibDll.cpp" \
   "$FORK/ThirdParty/MAC_SDK/Source/MACLibDll/MACLibDll.h" \
   "$MACSRC/Source/MACLibDll/"
# The override replaces the stock MACDll.cpp in the shared source list,
# applied to the build-scratch copy only (the Windows flow's equivalent
# swap happens through the pinned vcxproj override).
sed -i 's|Source/MACDll/MACDll.cpp|Source/MACLibDll/MACLibDll.cpp|' "$MACSRC/CMakeLists.txt"
cmake -S "$MACSRC" -B "$BUILD/mac" -DCMAKE_BUILD_TYPE=Release \
  -DBUILD_SHARED=ON -DBUILD_UTIL=OFF -DCMAKE_CXX_FLAGS="-DEXCLUDE_CIO" >/dev/null
cmake --build "$BUILD/mac" -j"$JOBS" >/dev/null
cp "$(readlink -f "$BUILD/mac/libMAC.so")" "$OUT/MACLibDll.so"

echo "== LAME (pinned lame-3.100 archive, fork ThirdParty)"
LAMESRC=$BUILD/lame-src
if [ ! -e "$LAMESRC/.built" ]; then
  rm -rf "$LAMESRC"
  mkdir -p "$LAMESRC"
  tar xzf "$FORK/ThirdParty/lame/lame-3.100.tar.gz" -C "$LAMESRC" --strip-components=1
  (cd "$LAMESRC" && ./configure --disable-static --enable-shared \
     --disable-frontend --disable-decoder --quiet >/dev/null && make -j"$JOBS" >/dev/null)
  touch "$LAMESRC/.built"
fi
cp "$(readlink -f "$LAMESRC/libmp3lame/.libs/libmp3lame.so")" "$OUT/libmp3lame.so"

strip --strip-unneeded "$OUT/libFLAC_dynamic.so" "$OUT/wavpackdll.so" "$OUT/MACLibDll.so" "$OUT/libmp3lame.so"

python3 - <<'PYEOF'
import hashlib, json, os
out = 'obj/native'
entries = []
for name in sorted(os.listdir(out)):
    if not name.endswith('.so'):
        continue
    path = os.path.join(out, name)
    digest = hashlib.sha256(open(path, 'rb').read()).hexdigest()
    entries.append({
        'file': name,
        'sha256': digest,
        'bytes': os.path.getsize(path),
    })
manifest = {'schemaVersion': 1, 'libraries': entries}
with open(os.path.join(out, 'native-codecs.json'), 'w') as f:
    json.dump(manifest, f, indent=2)
    f.write('\n')
for e in entries:
    print(f"  {e['file']}: {e['bytes']} bytes sha256 {e['sha256'][:16]}...")
PYEOF

echo "build-native-codecs: done -> $OUT"
