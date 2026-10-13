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
REMOTE_DIR="/server/game/bin/win64/managed/plugins"

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

LFTP_PASSWORD="$(security find-generic-password -a "$DW_USER" -s 'deadworks-sftp' -w)"
export LFTP_PASSWORD

if [[ -f "$BACKUP_DIR/RiftRoulette.dll" && -f "$BACKUP_DIR/CrystalCrackers.dll" ]]; then
  echo "error: $STAMP holds both mode DLLs; only one may be on the server. Remove one from $BACKUP_DIR first." >&2
  exit 1
fi

# Restoring a DLL must remove the copy it conflicts with so both never load:
# its renamed copy, or the other mode DLL.
conflicts_with() {
  case "$1" in
    RiftRumble.dll) echo "RiftRoulette.dll" ;;
    RiftRoulette.dll) echo "CrystalCrackers.dll" ;;
    CrystalCrackers.dll) echo "RiftRoulette.dll" ;;
  esac
}

echo "Restoring $STAMP to $REMOTE_DIR ..."
for dll in "${DLLS[@]}"; do
  name="$(basename "$dll")"
  other="$(conflicts_with "$name")"
  if [[ -n "$other" ]]; then
    lftp --env-password -u "$DW_USER" "sftp://$DW_HOST:$DW_PORT" \
      -e "rm '$REMOTE_DIR/$other'; bye" >/dev/null 2>&1 || true
    echo "  removed $other (it can't load alongside $name)"
  fi
done

for dll in "${DLLS[@]}"; do
  name="$(basename "$dll")"
  lftp --env-password -u "$DW_USER" "sftp://$DW_HOST:$DW_PORT" \
    -e "set cmd:fail-exit yes; put '$dll' -o '$REMOTE_DIR/$name'; bye"
  echo "  restored $name"
done

echo "Rollback OK. Plugins without a backup in $STAMP were left as they are."
