#!/usr/bin/env bash
# Rebuild the knowledge base: Deadworks API index, graph + tree + indexes, docs webpage.
# Offline except the first .NET run; never touches the server. See knowledge.sh.md.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LIB_DIR="${DeadworksLibDir:-$ROOT/lib}"
if [[ -f "$ROOT/../Directory.Build.props" && -z "${DeadworksLibDir:-}" ]]; then
  LIB_DIR="$ROOT/../lib"
fi

if [[ -f "$LIB_DIR/DeadworksManaged.Api.dll" ]]; then
  dotnet run "$ROOT/scripts/api-index.cs" -- "$LIB_DIR" "$ROOT/knowledge/generated/deadworks-api.md"
else
  echo "note: no Deadworks API in $LIB_DIR (scripts/fetch-deadworks.sh); keeping the existing API index."
fi
python3 "$ROOT/scripts/knowledge-graph.py"
python3 "$ROOT/scripts/docs-site.py"
echo "Knowledge OK. Open site/index.html, or read knowledge/README.md."
