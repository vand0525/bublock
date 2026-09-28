#!/usr/bin/env bash
# Build all Bublock plugins. Does NOT deploy; scripts/deploy.sh uploads.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SLN="$ROOT/Bublock.sln"

# Outside the deadworks/ workspace (no ../Directory.Build.props), build against
# lib/ from scripts/fetch-deadworks.sh.
if [[ ! -f "$ROOT/../Directory.Build.props" ]]; then
  export DeadworksLibDir="${DeadworksLibDir:-$ROOT/lib}"
fi

if [[ "${1:-}" == "--deploy" ]] || [[ "${DeployPlugins:-}" == "true" ]]; then
  echo "error: update.sh is build-only. Upload with scripts/deploy.sh --confirm" >&2
  echo "(backs up the server DLLs first; see Bublock/scripts/deploy.sh.md)." >&2
  exit 1
fi

echo "Building Bublock solution (Release, no deploy)..."
dotnet build "$SLN" -c Release
echo "Build OK. No files were uploaded."
