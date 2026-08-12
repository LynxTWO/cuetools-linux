#!/usr/bin/env bash
# Builds the .deb from an existing NativeAOT publish (run
# ./eng/build.sh --publish first) and enforces the D-025 size gate:
# package download <= 30 MB, installed payload <= 60 MB. The gate failing
# fails the build; a bigger app needs a logged superseding decision, not a
# quiet budget bump (ADD guardrail 9).
set -euo pipefail
cd "$(dirname "$0")/.."

PUBLISH=src/CUETools.Linux.App/bin/Release/net10.0/linux-x64/publish
OUT=bin/packages
STAGE="$OUT/cuetools-linux-deb"

[ -x "$PUBLISH/CUETools.Linux.App" ] || {
  echo "ERROR: no publish output; run ./eng/build.sh --publish first" >&2
  exit 1
}

# Single-source the version from the csproj; deb pre-release ordering wants
# a tilde (0.1.0-alpha -> 0.1.0~alpha).
VERSION=$(grep -oPm1 '(?<=<Version>)[^<]+' src/CUETools.Linux.App/CUETools.Linux.App.csproj)
DEB_VERSION=${VERSION/-/\~}

rm -rf "$STAGE"
mkdir -p "$STAGE/DEBIAN" \
         "$STAGE/usr/lib/cuetools-linux" \
         "$STAGE/usr/bin" \
         "$STAGE/usr/share/applications" \
         "$STAGE/usr/share/icons/hicolor/256x256/apps" \
         "$STAGE/usr/share/doc/cuetools-linux"

install -m 755 "$PUBLISH/CUETools.Linux.App" "$STAGE/usr/lib/cuetools-linux/cuetools-linux"
for so in "$PUBLISH"/*.so; do
  install -m 644 "$so" "$STAGE/usr/lib/cuetools-linux/"
done
ln -s ../lib/cuetools-linux/cuetools-linux "$STAGE/usr/bin/cuetools-linux"
install -m 644 eng/packaging/cuetools-linux.desktop "$STAGE/usr/share/applications/"
install -m 644 eng/packaging/cuetools-linux.png \
  "$STAGE/usr/share/icons/hicolor/256x256/apps/cuetools-linux.png"

cat > "$STAGE/usr/share/doc/cuetools-linux/copyright" <<EOF
CUETools Linux
Copyright (C) 2006-2008 Moitah
Copyright (C) 2008-2026 Gregory S. Chudov and CUETools contributors
Copyright (C) 2026 Daniel Boyd and CUETools Linux contributors

Licensed under the GNU General Public License, version 2 or (at your
option) any later version. See /usr/share/common-licenses/GPL-2.
Source: https://github.com/LynxTWO/cuetools-linux
EOF

INSTALLED_KB=$(du -sk "$STAGE/usr" | cut -f1)
cat > "$STAGE/DEBIAN/control" <<EOF
Package: cuetools-linux
Version: $DEB_VERSION
Architecture: amd64
Maintainer: Daniel Boyd <usaf.the.boyd@gmail.com>
Installed-Size: $INSTALLED_KB
Depends: libx11-6, libice6, libsm6, libfontconfig1
Section: sound
Priority: optional
Homepage: https://github.com/LynxTWO/cuetools-linux
Description: Verify, repair, and convert lossless CD rips
 Native Linux desktop app bringing the CUETools 2026 experience to
 Linux: verification against AccurateRip and the CUETools Database,
 CTDB parity repair, and lossless conversion, with honest, immutable
 evidence for every job.
EOF

mkdir -p "$OUT"
DEB="$OUT/cuetools-linux_${DEB_VERSION}_amd64.deb"
dpkg-deb --build --root-owner-group "$STAGE" "$DEB" > /dev/null

DEB_MB=$(( $(stat -c%s "$DEB") / 1048576 ))
INSTALLED_MB=$(( INSTALLED_KB / 1024 ))
echo "package: $DEB (${DEB_MB} MB download, ${INSTALLED_MB} MB installed)"
[ "$DEB_MB" -le 30 ] || { echo "ERROR: size gate: deb ${DEB_MB} MB > 30 MB (D-025)" >&2; exit 1; }
[ "$INSTALLED_MB" -le 60 ] || { echo "ERROR: size gate: installed ${INSTALLED_MB} MB > 60 MB (D-025)" >&2; exit 1; }
echo "size gate: within D-025 budgets"
