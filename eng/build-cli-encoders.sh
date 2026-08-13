#!/usr/bin/env bash
# Build the curated command-line encoders for Linux from pinned sources
# (SLICE-007 increment B, D-047). The archives and patches mirror the fork's
# Windows curated-encoder manifest (eng/release/external-command-encoders
# .json); every download is SHA-256-verified before use, and the outputs are
# recorded in obj/encoders/linux-encoders.json for the app's runtime
# validation. Executable names match the catalog's cross-head encoder
# identities (opusenc.exe, mpcenc.exe, oggenc.exe) - the .exe name is
# identity, not a format claim; these are ELF binaries.
#
# Linux-recorded divergence: the ogg entry is built from official
# vorbis-tools 1.4.2 + libvorbis 1.3.7 (the Windows bundled binary is the
# rarewares oggenc2 build, whose source drop is MSVC-only). The engine's
# oggenc.exe entry uses only standard oggenc flags, so behavior matches.

set -euo pipefail
cd "$(dirname "$0")/.."

ROOT=$PWD
FORK=extern/cuetools_2026
STAGED=$FORK/obj/vendor-sources/current/ThirdParty
BUILD=obj/enc-build
DL=$BUILD/dl
PREFIX=$ROOT/$BUILD/prefix
OUT=obj/encoders
JOBS=$(nproc)

mkdir -p "$DL" "$OUT"

fetch() { # url sha256
  local url=$1 sha=$2 file
  file=$DL/$(basename "$url")
  [ -f "$file" ] || curl -sL -o "$file" "$url"
  echo "$sha  $file" | sha256sum -c --quiet
}

# ---- pinned inputs (URLs + hashes mirror the fork manifest; additions for
# ---- the Linux ogg build are pinned here and recorded in the output manifest)
fetch "https://downloads.xiph.org/releases/ogg/libogg-1.3.6.tar.gz" \
      "83e6704730683d004d20e21b8f7f55dcb3383cdf84c0daedf30bde175f774638"
fetch "https://downloads.xiph.org/releases/opus/opus-1.6.1.tar.gz" \
      "6ffcb593207be92584df15b32466ed64bbec99109f007c82205f0194572411a1"
fetch "https://downloads.xiph.org/releases/opus/libopusenc-0.3.tar.gz" \
      "f616d3aff9b2034547894ccb8ab56c36cf1a4acb0d922c5d7119f97bbe58642c"
fetch "https://archive.mozilla.org/pub/opus/opus-tools-0.2.tar.gz" \
      "b4e56cb00d3e509acfba9a9b627ffd8273b876b4e2408642259f6da28fa0ff86"
fetch "https://deb.debian.org/debian/pool/main/libm/libmpc/libmpc_0.1~r495.orig.tar.gz" \
      "5182d7a30eec1b338cafc9c1706d663a6c8dc217b1d90187054512074f662202"
fetch "https://downloads.xiph.org/releases/vorbis/libvorbis-1.3.7.tar.gz" \
      "0e982409a9c3fc82ee06e08205b1355e5c6aa4c36bca58146ef399621b0ce5ab"
fetch "https://downloads.xiph.org/releases/vorbis/vorbis-tools-1.4.2.tar.gz" \
      "db7774ec2bf2c939b139452183669be84fda5774d6400fc57fde37f77624f0b0"

untar() { local a=$1 d=$2; rm -rf "$d"; mkdir -p "$d"; tar xzf "$a" -C "$d" --strip-components=1; }

if [ ! -e "$PREFIX/.complete" ]; then
  echo "== static support libraries (prefix)"
  untar "$DL/libogg-1.3.6.tar.gz" "$BUILD/libogg"
  (cd "$BUILD/libogg" && ./configure --prefix="$PREFIX" --disable-shared --enable-static --quiet >/dev/null && make -j"$JOBS" install >/dev/null 2>&1)

  untar "$DL/opus-1.6.1.tar.gz" "$BUILD/opus"
  (cd "$BUILD/opus" && patch -p1 < "$ROOT/$FORK/ThirdParty/opus-1.6.1-cuetools.patch" >/dev/null && \
   ./configure --prefix="$PREFIX" --disable-shared --enable-static --disable-doc --disable-extra-programs --quiet >/dev/null && make -j"$JOBS" install >/dev/null 2>&1)

  untar "$DL/libopusenc-0.3.tar.gz" "$BUILD/libopusenc"
  (cd "$BUILD/libopusenc" && patch -p1 < "$ROOT/$FORK/ThirdParty/libopusenc-0.3-cuetools.patch" >/dev/null && \
   PKG_CONFIG_PATH=$PREFIX/lib/pkgconfig ./configure --prefix="$PREFIX" --disable-shared --enable-static --disable-doc --quiet >/dev/null && make -j"$JOBS" install >/dev/null 2>&1)

  untar "$DL/libvorbis-1.3.7.tar.gz" "$BUILD/libvorbis"
  (cd "$BUILD/libvorbis" && PKG_CONFIG_PATH=$PREFIX/lib/pkgconfig ./configure --prefix="$PREFIX" --disable-shared --enable-static --quiet >/dev/null 2>&1 && make -j"$JOBS" install >/dev/null 2>&1)

  [ -d "$STAGED/flac" ] || { echo "ERROR: vendor staging missing; run eng/build.sh first" >&2; exit 1; }
  cmake -S "$STAGED/flac" -B "$BUILD/flac-static" -DCMAKE_BUILD_TYPE=Release \
    -DBUILD_SHARED_LIBS=OFF -DWITH_OGG=OFF -DBUILD_PROGRAMS=OFF -DBUILD_EXAMPLES=OFF \
    -DBUILD_TESTING=OFF -DBUILD_DOCS=OFF -DBUILD_CXXLIBS=OFF -DINSTALL_MANPAGES=OFF \
    -DCMAKE_INSTALL_PREFIX="$PREFIX" >/dev/null
  cmake --build "$BUILD/flac-static" -j"$JOBS" >/dev/null 2>&1
  cmake --install "$BUILD/flac-static" >/dev/null
  touch "$PREFIX/.complete"
fi

echo "== opusenc (opus-tools 0.2; opusfile/opusurl stubbed - only opusenc is built)"
untar "$DL/opus-tools-0.2.tar.gz" "$BUILD/opus-tools"
(cd "$BUILD/opus-tools" && STUB="-I$PREFIX/include" && \
 PKG_CONFIG_PATH=$PREFIX/lib/pkgconfig OPUSFILE_CFLAGS="$STUB" OPUSFILE_LIBS="-L$PREFIX/lib" \
 OPUSURL_CFLAGS="$STUB" OPUSURL_LIBS="-L$PREFIX/lib" \
 ./configure --prefix="$PREFIX" --quiet >/dev/null 2>&1 && make -j"$JOBS" opusenc >/dev/null 2>&1)
cp "$BUILD/opus-tools/opusenc" "$OUT/opusenc.exe"

echo "== mpcenc (musepack r495 + CUETools patch)"
untar "$DL/libmpc_0.1~r495.orig.tar.gz" "$BUILD/mpc"
(cd "$BUILD/mpc" && patch -p1 < "$ROOT/$FORK/ThirdParty/musepack-r495-cuetools.patch" >/dev/null)
cmake -S "$BUILD/mpc" -B "$BUILD/mpc-build" -DCMAKE_BUILD_TYPE=Release >/dev/null 2>&1 || true
cmake --build "$BUILD/mpc-build" -j"$JOBS" --target mpcenc >/dev/null 2>&1
cp "$BUILD/mpc-build/mpcenc/mpcenc" "$OUT/mpcenc.exe"

echo "== oggenc (vorbis-tools 1.4.2, official; Linux stand-in for the Windows oggenc2 build)"
untar "$DL/vorbis-tools-1.4.2.tar.gz" "$BUILD/vorbis-tools"
(cd "$BUILD/vorbis-tools" && \
 PKG_CONFIG_PATH=$PREFIX/lib/pkgconfig FLAC_CFLAGS="-I$PREFIX/include" FLAC_LIBS="-L$PREFIX/lib -lFLAC -lm" \
 ./configure --prefix="$PREFIX" --without-speex --without-kate --disable-ogg123 \
   --disable-vorbiscomment --disable-vcut --disable-ogginfo --quiet >/dev/null 2>&1 && \
 make -C share >/dev/null 2>&1 && make -j"$JOBS" -C oggenc oggenc >/dev/null 2>&1)
cp "$BUILD/vorbis-tools/oggenc/oggenc" "$OUT/oggenc.exe"

strip --strip-unneeded "$OUT/opusenc.exe" "$OUT/mpcenc.exe" "$OUT/oggenc.exe"

python3 - <<'PYEOF'
import hashlib, json, os, subprocess
out = 'obj/encoders'
meta = {
    'opusenc.exe': {
        'version': 'opus-tools 0.2 + libopusenc 0.3 + libopus 1.6.1 (CUETools patches) + libogg 1.3.6',
        'license': 'BSD-2-Clause (opusenc) and BSD-3-Clause (libopus, libopusenc, libogg)',
    },
    'mpcenc.exe': {
        'version': 'Musepack r495 + CUETools patch (SV8)',
        'license': 'LGPL-2.1-or-later and BSD-3-Clause components',
    },
    'oggenc.exe': {
        'version': 'vorbis-tools 1.4.2 + libvorbis 1.3.7 + libogg 1.3.6',
        'license': 'GPL-2.0 (oggenc); BSD-3-Clause (libvorbis, libogg)',
    },
}
entries = []
for name in sorted(meta):
    path = os.path.join(out, name)
    digest = hashlib.sha256(open(path, 'rb').read()).hexdigest()
    entries.append({
        'file': name,
        'sha256': digest,
        'bytes': os.path.getsize(path),
        'version': meta[name]['version'],
        'license': meta[name]['license'],
    })
manifest = {'schemaVersion': 1, 'encoders': entries}
with open(os.path.join(out, 'linux-encoders.json'), 'w') as f:
    json.dump(manifest, f, indent=2)
    f.write('\n')
for e in entries:
    print(f"  {e['file']}: {e['bytes']} bytes sha256 {e['sha256'][:16]}...")
PYEOF

echo "build-cli-encoders: done -> $OUT"
