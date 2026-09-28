#!/usr/bin/env bash
# Re-upload a backup set taken by deploy.sh to the Deadworks server.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BACKUP_ROOT="$(cd "$ROOT/.." && pwd)/server-backups"

if [[ ! -f "$ROOT/scripts/server.env" ]]; then
  echo "error: copy scripts/server.env.example to scripts/server.env and fill it in." >&2
  exit 1
fi
source "$ROOT/scripts/server.env"
REMOTE_GAME="${DW_REMOTE_GAME-/server/game}"
REMOTE_DIR="$REMOTE_GAME/bin/win64/managed/plugins"

STAMP="${1:-}"
if [[ -z "$STAMP" ]]; then
  echo "usage: rollback.sh <stamp>   (stamps: ls $BACKUP_ROOT)" >&2
  exit 1
fi

BACKUP_DIR="$BACKUP_ROOT/$STAMP"
shopt -s nullglob
DLLS=("$BACKUP_DIR"/*.dll)

if [[ ${#DLLS[@]} -eq 0 ]]; then
  echo "error: no DLLs in $BACKUP_DIR" >&2
  exit 1
fi

if ! command -v lftp >/dev/null 2>&1; then
  echo "error: lftp is not installed (brew install lftp)." >&2
  exit 1
fi

source "$ROOT/scripts/sftp-password.sh"

# Restoring a DLL under its pre-rename name must remove the renamed copy so both never load.
renamed_to() {
  case "$1" in
    RiftRumble.dll) echo "RiftRoulette.dll" ;;
  esac
}

echo "Restoring $STAMP to $REMOTE_DIR ..."
for dll in "${DLLS[@]}"; do
  name="$(basename "$dll")"
  new_name="$(renamed_to "$name")"
  if [[ -n "$new_name" ]]; then
    lftp --env-password -u "$DW_USER" "sftp://$DW_HOST:$DW_PORT" \
      -e "rm '$REMOTE_DIR/$new_name'; bye" >/dev/null 2>&1 || true
    echo "  removed $new_name (restoring its old name $name)"
  fi
done

for dll in "${DLLS[@]}"; do
  name="$(basename "$dll")"
  lftp --env-password -u "$DW_USER" "sftp://$DW_HOST:$DW_PORT" \
    -e "set cmd:fail-exit yes; put '$dll' -o '$REMOTE_DIR/$name'; bye"
  echo "  restored $name"
done

echo "Rollback OK. Plugins without a backup in $STAMP were left as they are."
