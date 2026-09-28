#!/usr/bin/env bash
# Download the Deadworks API DLLs the build compiles against into lib/ (git-ignored).
# For a checkout with no workspace ../lib (CI, a plain clone). See fetch-deadworks.sh.md.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="${1:-${DEADWORKS_VERSION:-v0.4.18}}"
LIB_DIR="${DeadworksLibDir:-$ROOT/lib}"
FILES=(DeadworksManaged.Api.dll Google.Protobuf.dll DeadworksManaged.Api.xml)

if [[ -f "$LIB_DIR/.version" && "$(cat "$LIB_DIR/.version")" == "$VERSION" ]]; then
  echo "Deadworks $VERSION already in $LIB_DIR."
  exit 0
fi

URL="https://github.com/Deadworks-net/deadworks/releases/download/$VERSION/deadworks-$VERSION.zip"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

echo "Downloading Deadworks $VERSION ..."
curl -fsSL "$URL" -o "$TMP/deadworks.zip"

mkdir -p "$LIB_DIR"
python3 - "$TMP/deadworks.zip" "$LIB_DIR" "${FILES[@]}" <<'EOF'
import os, sys, zipfile
archive, dest, names = sys.argv[1], sys.argv[2], sys.argv[3:]
with zipfile.ZipFile(archive) as z:
    for name in names:
        member = f"game/bin/win64/managed/{name}"
        with z.open(member) as src, open(os.path.join(dest, name), "wb") as out:
            out.write(src.read())
EOF

echo "$VERSION" > "$LIB_DIR/.version"
echo "Deadworks $VERSION -> $LIB_DIR (${FILES[*]})."
