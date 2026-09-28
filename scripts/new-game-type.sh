#!/usr/bin/env bash
# Scaffold a new game type (its own plugin DLL on the engine: Shared + Modules) from templates/.
# Usage: new-game-type.sh <Name> <prefix> ["Title"]   e.g. new-game-type.sh Grifball grif "Grifball"
# See new-game-type.sh.md.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
NAME="${1:-}"
PREFIX="${2:-}"
TITLE="${3:-$NAME}"

if [[ ! "$NAME" =~ ^[A-Z][A-Za-z0-9]+$ || ! "$PREFIX" =~ ^[a-z][a-z0-9]{1,7}$ ]]; then
  echo "usage: new-game-type.sh <Name> <prefix> [\"Title\"]" >&2
  echo "  Name: PascalCase project / DLL name (Grifball); prefix: 2-8 lowercase letters for admin commands (grif_status)." >&2
  exit 1
fi

for taken in "$ROOT/$NAME" "$ROOT/Tests/$NAME.Tests"; do
  if [[ -e "$taken" ]]; then
    echo "error: $taken already exists." >&2
    exit 1
  fi
done

if grep -rqs "Command(\"${PREFIX}_" "$ROOT"/*/*.cs; then
  echo "error: admin prefix '${PREFIX}_' is already used by another game type." >&2
  exit 1
fi

NAMELOWER="$(echo "$NAME" | tr '[:upper:]' '[:lower:]')"

render() {
  local from="$1" to="$2"
  mkdir -p "$to"
  (cd "$from" && find . -type f) | while read -r rel; do
    local target="$to/${rel//__NAME__/$NAME}"
    mkdir -p "$(dirname "$target")"
    sed -e "s/__NAMELOWER__/$NAMELOWER/g" -e "s/__NAME__/$NAME/g" -e "s/__PREFIX__/$PREFIX/g" -e "s/__TITLE__/$TITLE/g" \
      "$from/$rel" > "$target"
  done
}

render "$ROOT/templates/game-type" "$ROOT/$NAME"
render "$ROOT/templates/game-type-tests" "$ROOT/Tests/$NAME.Tests"
echo "Created $NAME/ and Tests/$NAME.Tests/ from templates/."

if command -v dotnet >/dev/null 2>&1; then
  dotnet sln "$ROOT/Bublock.sln" add "$ROOT/$NAME/$NAME.csproj" "$ROOT/Tests/$NAME.Tests/$NAME.Tests.csproj" >/dev/null
  echo "Added both projects to Bublock.sln."
else
  echo "note: dotnet not on PATH; add $NAME/$NAME.csproj and Tests/$NAME.Tests/$NAME.Tests.csproj to Bublock.sln yourself." >&2
fi

cat <<EOF

Next:
  1. Describe $TITLE in $NAME/FEATURE.md and change $NAME/${NAME}Rules.cs (what scores, match length, text).
  2. Wire extra engine modules in $NAME/$NAME.csproj and $NAME/${NAME}Service.cs (RandomLoadout, WorldText, ...).
  3. New arena? Edit $NAME/Data/arena.json, then: python3 scripts/check-arena.py $NAME/Data/arena.json
  4. scripts/update.sh && scripts/test.sh
  5. Run it: DW_PLUGINS="$NAME DevTools CleanSlate" and DW_PARKED="<other game types>" in scripts/server.env,
     scripts/deploy.sh --confirm (with approval), then restart the server.
  6. scripts/knowledge.sh (the graph picks up the new game type).
EOF
