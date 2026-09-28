#!/usr/bin/env bash
# Run Bublock local tests (no server needed, nothing uploaded).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

# Outside the deadworks/ workspace (no ../Directory.Build.props), build against
# lib/ from scripts/fetch-deadworks.sh.
if [[ ! -f "$ROOT/../Directory.Build.props" ]]; then
  export DeadworksLibDir="${DeadworksLibDir:-$ROOT/lib}"
fi

echo "Running Bublock tests..."
for project in "$ROOT"/Tests/*/*.Tests.csproj; do
  dotnet test "$project" -c Release
done
echo "Tests OK."
