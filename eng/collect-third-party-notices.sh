#!/usr/bin/env bash
# Collect the third-party license texts that must accompany a shipped package,
# copying them from the pinned sources the build actually compiled. Nothing
# here authors or paraphrases a license: every text is the upstream file.
#
# Why this exists: the packages ship vendored codecs (D-042) and curated CLI
# encoders (D-047). Several carry copyleft terms - LAME is LGPL, oggenc is
# GPL-2.0, mpcenc carries LGPL components - so distributing the binaries
# without their license texts and a source offer is a licence breach the
# moment the payload is actually present. The payload was missing until
# 2026-08-15, which is the only reason this was not already a live problem.
#
# Usage: collect-third-party-notices.sh <destination-directory>
set -euo pipefail
DEST="${1:?usage: collect-third-party-notices.sh <destination-directory>}"
cd "$(dirname "$0")/.."

VENDOR=extern/cuetools_2026/obj/vendor-sources/current/ThirdParty
NATIVE_BUILD=obj/native-build
FORK_LICENSES=extern/cuetools_2026/eng/release/licenses

mkdir -p "$DEST/licenses"

copied=0
copy_license() {
  local src="$1" name="$2"
  if [ -f "$src" ]; then
    install -m 644 "$src" "$DEST/licenses/$name"
    copied=$((copied + 1))
  else
    echo "WARNING: license source missing: $src (expected for $name)" >&2
  fi
}

# Vendored native codecs.
copy_license "$VENDOR/flac/COPYING.Xiph"            "libFLAC-1.5.0-COPYING.Xiph.txt"
copy_license "$VENDOR/WavPack/COPYING"              "WavPack-5.9.0-COPYING.txt"
copy_license "$NATIVE_BUILD/mac-src/License.txt"    "MonkeysAudio-13.20-License.txt"
copy_license "$FORK_LICENSES/LAME-3.100-LICENSE.txt" "LAME-3.100-LICENSE.txt"
copy_license "$FORK_LICENSES/GNU-LGPL-2.0.txt"      "GNU-LGPL-2.0.txt"

# Curated CLI encoders. Prefer the text from the source tree the encoder was
# actually built from; fall back to the fork's recorded copy.
ENC_BUILD=obj/enc-build
copy_license "$FORK_LICENSES/OpusTools-opusenc-BSD-2-Clause.txt" "OpusTools-opusenc-BSD-2-Clause.txt"
copy_license "$FORK_LICENSES/libopus-1.6.1-COPYING.txt"          "libopus-1.6.1-COPYING.txt"
copy_license "$FORK_LICENSES/libopusenc-0.3-BSD-3-Clause.txt"    "libopusenc-0.3-BSD-3-Clause.txt"
copy_license "$FORK_LICENSES/libogg-1.3.6-BSD-3-Clause.txt"      "libogg-1.3.6-BSD-3-Clause.txt"
# oggenc itself is GPL-2.0 (vorbis-tools) over BSD-licensed libvorbis.
copy_license "$ENC_BUILD/vorbis-tools-1.4.2/COPYING" "vorbis-tools-1.4.2-COPYING.txt"
copy_license "$ENC_BUILD/libvorbis-1.3.7/COPYING"    "libvorbis-1.3.7-COPYING.txt"
# Musepack: the SV8 encoder tree carries its licence under libmpcdec.
copy_license "$ENC_BUILD/mpc/libmpcdec/COPYING" "Musepack-r495-COPYING.txt"

# Managed dependencies that ship as binaries beside the app.
copy_license "$VENDOR/taglib-sharp/COPYING" "TagLibSharp-COPYING.txt"

[ "$copied" -gt 0 ] || { echo "ERROR: collected no license texts" >&2; exit 1; }

# The notices index and the written offer. Component rows are generated from
# the manifests so a payload change cannot leave this list stale.
python3 - "$DEST/THIRD-PARTY-NOTICES.md" << 'PY'
import json, os, sys

dest = sys.argv[1]
rows = []

native = "obj/native/native-codecs.json"
if os.path.exists(native):
    for lib in json.load(open(native)).get("libraries", []):
        rows.append((
            lib["file"],
            lib.get("version", "see manifest"),
            lib.get("license", "see licenses/ directory"),
            lib.get("source", ""),
        ))

encoders = "obj/encoders/linux-encoders.json"
if os.path.exists(encoders):
    for enc in json.load(open(encoders)).get("encoders", []):
        rows.append((
            enc.get("file", ""),
            enc.get("version", "see manifest"),
            enc.get("license", "see licenses/ directory"),
            enc.get("source", ""),
        ))

lines = [
    "# Third-party notices",
    "",
    "CUETools Linux ships the binaries listed below. Each one keeps its own",
    "licence; the full texts are in the `licenses/` directory beside this file.",
    "",
    "| Binary | Version | Licence |",
    "| --- | --- | --- |",
]
for f, v, l, _ in rows:
    lines.append("| `%s` | %s | %s |" % (f, v, l))

lines += [
    "",
    "The application itself, and the CUETools engine it embeds, are licensed",
    "under the GNU General Public License version 2 or later.",
    "",
    "## Written offer of source code",
    "",
    "Some of the binaries above carry copyleft terms (LAME is LGPL, `oggenc`",
    "is GPL-2.0, `mpcenc` includes LGPL components). Their complete source is",
    "the exact upstream release each one was built from, pinned by version and",
    "checksum in this project's build scripts:",
    "",
    "- `eng/build-native-codecs.sh` builds the vendored codecs.",
    "- `eng/build-cli-encoders.sh` builds the curated command-line encoders.",
    "- `eng/release/native-dependencies.json` in the CUETools 2026 fork records",
    "  each component's version, licence, and upstream source.",
    "",
    "Those scripts, and the pinned sources they fetch, are published at",
    "https://github.com/LynxTWO/cuetools-linux and",
    "https://github.com/LynxTWO/cuetools_2026.",
    "",
    "If you would rather receive the corresponding source on physical media,",
    "or the upstream location has moved, open an issue at",
    "https://github.com/LynxTWO/cuetools-linux/issues and it will be provided",
    "for as long as this release is distributed.",
    "",
]
open(dest, "w").write("\n".join(lines))
print("notices: %d component rows" % len(rows))
PY

echo "third-party notices: $copied licence texts collected into $DEST/licenses"
