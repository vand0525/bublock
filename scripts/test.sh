#!/usr/bin/env bash
# Run Bublock local tests (no server needed, nothing uploaded).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "Running Bublock tests..."
for project in "$ROOT"/Tests/*/*.Tests.csproj; do
  dotnet test "$project" -c Release
done
echo "Tests OK."
