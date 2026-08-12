#!/usr/bin/env bash
# Builds the AppImage from an existing NativeAOT publish (run
# ./eng/build.sh --publish first). appimagetool is fetched from its
# upstream continuous release and byte-pinned: a hash mismatch fails the
# build rather than running unexpected tool bytes (update the pin
# deliberately alongside the recorded version). Enforces the D-025 size
# gate: package <= 30 MB.
set -euo pipefail
cd "$(dirname "$0")/.."

PUBLISH=src/CUETools.Linux.App/bin/Release/net10.0/linux-x64/publish
OUT=bin/packages
APPDIR="$OUT/CUEToolsLinux.AppDir"
TOOL_CACHE="bin/tools/appimagetool"
# appimagetool continuous build 295, git 8c8c91f, built 2025-12-04,
# fetched and hashed 2026-08-12.
TOOL_URL="https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
TOOL_SHA256="a6d71e2b6cd66f8e8d16c37ad164658985e0cf5fcaa950c90a482890cb9d13e0"

[ -x "$PUBLISH/CUETools.Linux.App" ] || {
  echo "ERROR: no publish output; run ./eng/build.sh --publish first" >&2
  exit 1
}

mkdir -p "$(dirname "$TOOL_CACHE")"
if [ ! -f "$TOOL_CACHE" ] || ! echo "$TOOL_SHA256  $TOOL_CACHE" | sha256sum -c --quiet - 2>/dev/null; then
  curl -sSL --retry 5 -o "$TOOL_CACHE" "$TOOL_URL"
  echo "$TOOL_SHA256  $TOOL_CACHE" | sha256sum -c --quiet - || {
    echo "ERROR: appimagetool hash mismatch; refusing to run it" >&2
    exit 1
  }
  chmod +x "$TOOL_CACHE"
fi

VERSION=$(grep -oPm1 '(?<=<Version>)[^<]+' src/CUETools.Linux.App/CUETools.Linux.App.csproj)

rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/share/icons/hicolor/256x256/apps"
install -m 755 "$PUBLISH/CUETools.Linux.App" "$APPDIR/usr/bin/cuetools-linux"
for so in "$PUBLISH"/*.so; do
  install -m 644 "$so" "$APPDIR/usr/bin/"
done
install -m 644 eng/packaging/cuetools-linux.desktop "$APPDIR/cuetools-linux.desktop"
install -m 644 eng/packaging/cuetools-linux.png "$APPDIR/cuetools-linux.png"
install -m 644 eng/packaging/cuetools-linux.png \
  "$APPDIR/usr/share/icons/hicolor/256x256/apps/cuetools-linux.png"
cat > "$APPDIR/AppRun" <<'EOF'
#!/bin/sh
HERE="$(dirname "$(readlink -f "$0")")"
exec "$HERE/usr/bin/cuetools-linux" "$@"
EOF
chmod +x "$APPDIR/AppRun"

APPIMAGE="$OUT/CUEToolsLinux-${VERSION}-x86_64.AppImage"
ARCH=x86_64 "$TOOL_CACHE" --appimage-extract-and-run "$APPDIR" "$APPIMAGE" > /dev/null 2>&1

SIZE_MB=$(( $(stat -c%s "$APPIMAGE") / 1048576 ))
echo "package: $APPIMAGE (${SIZE_MB} MB)"
[ "$SIZE_MB" -le 30 ] || { echo "ERROR: size gate: AppImage ${SIZE_MB} MB > 30 MB (D-025)" >&2; exit 1; }
echo "size gate: within D-025 budget"
