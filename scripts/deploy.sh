#!/usr/bin/env bash
# Upload the Bublock plugin DLLs to the Deadworks server.
# Backs up the server's current copies first; roll back with rollback.sh <stamp>.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BACKUP_ROOT="$(cd "$ROOT/.." && pwd)/server-backups"

if [[ ! -f "$ROOT/scripts/server.env" ]]; then
  echo "error: copy scripts/server.env.example to scripts/server.env and fill it in." >&2
  exit 1
fi
source "$ROOT/scripts/server.env"
REMOTE_DIR="/server/game/bin/win64/managed/plugins"
PLUGINS=(RiftRoulette DevTools CleanSlate)
# Old DLL names still on the server after a rename; deleted so two copies never load.
RETIRED_PLUGINS=(RiftRumble)
REMOTE_LOG_DIR="/server/game/bin/win64/bublock/logs"
REMOTE_ACCESS="/server/game/bin/win64/bublock/access.json"
LOCAL_ACCESS="$ROOT/server-data/access.json"

CONFIRMED=false
BACKUP=true
PUSH_ACCESS=false
for arg in "$@"; do
  case "$arg" in
    --confirm) CONFIRMED=true ;;
    --no-backup) BACKUP=false ;;
    --push-access) PUSH_ACCESS=true ;;
    *) CONFIRMED=false; break ;;
  esac
done

if [[ "$CONFIRMED" != true ]]; then
  echo "usage: deploy.sh --confirm [--no-backup] [--push-access]" >&2
  echo "Builds, backs up the server's ${PLUGINS[*]} DLLs, then uploads the Bublock builds." >&2
  exit 1
fi

if ! command -v lftp >/dev/null 2>&1; then
  echo "error: lftp is not installed (brew install lftp)." >&2
  exit 1
fi

"$ROOT/scripts/update.sh"

for plugin in "${PLUGINS[@]}"; do
  if [[ ! -f "$ROOT/$plugin/bin/Release/net10.0/$plugin.dll" ]]; then
    echo "error: missing build output $plugin/bin/Release/net10.0/$plugin.dll" >&2
    exit 1
  fi
done

LFTP_PASSWORD="$(security find-generic-password -a "$DW_USER" -s 'deadworks-sftp' -w)"
export LFTP_PASSWORD

sftp_run() {
  lftp --env-password -u "$DW_USER" "sftp://$DW_HOST:$DW_PORT" -e "set cmd:fail-exit yes; $1; bye"
}

STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
BACKUP_DIR="$BACKUP_ROOT/$STAMP"
mkdir -p "$BACKUP_DIR"

# The pull below overwrites the local access.json, so the edited copy is set aside first.
if [[ "$PUSH_ACCESS" == true ]]; then
  if [[ ! -f "$LOCAL_ACCESS" ]] || ! python3 -m json.tool "$LOCAL_ACCESS" >/dev/null; then
    echo "error: --push-access needs valid JSON in $LOCAL_ACCESS" >&2
    exit 1
  fi
  cp "$LOCAL_ACCESS" "$BACKUP_DIR/access.pushed.json"
fi

if [[ "$BACKUP" == true ]]; then
  echo "Backing up server plugins to $BACKUP_DIR ..."
  for plugin in "${PLUGINS[@]}" "${RETIRED_PLUGINS[@]}"; do
    if sftp_run "cls -1 '$REMOTE_DIR/$plugin.dll'" >/dev/null 2>&1; then
      sftp_run "get '$REMOTE_DIR/$plugin.dll' -o '$BACKUP_DIR/$plugin.dll'"
      echo "  backed up $plugin.dll"
    else
      echo "  note: no $plugin.dll on the server (nothing to back up)"
    fi
  done
else
  echo "Skipping backup (--no-backup)."
fi

echo "Pulling server logs and access.json before the upload ..."
if "$ROOT/scripts/pull-logs.sh"; then
  if [[ -f "$ROOT/server-data/access.json" ]]; then
    cp "$ROOT/server-data/access.json" "$BACKUP_DIR/access.json"
    echo "  saved access.json to $BACKUP_DIR"
  fi
else
  echo "  warning: pull-logs.sh failed; logs and access.json were not saved locally" >&2
fi

if [[ "$PUSH_ACCESS" == true ]]; then
  if [[ ! -f "$BACKUP_DIR/access.json" ]]; then
    echo "error: the server's access.json was not saved to $BACKUP_DIR; not replacing it" >&2
    cp "$BACKUP_DIR/access.pushed.json" "$LOCAL_ACCESS"
    exit 1
  fi
  sftp_run "put '$BACKUP_DIR/access.pushed.json' -o '$REMOTE_ACCESS'"
  cp "$BACKUP_DIR/access.pushed.json" "$LOCAL_ACCESS"
  echo "  uploaded access.json (server's previous copy: $BACKUP_DIR/access.json)"
fi

RETIRED_REMOVED=false
for plugin in "${RETIRED_PLUGINS[@]}"; do
  if sftp_run "cls -1 '$REMOTE_DIR/$plugin.dll'" >/dev/null 2>&1; then
    sftp_run "rm '$REMOTE_DIR/$plugin.dll'"
    RETIRED_REMOVED=true
    echo "  removed retired $plugin.dll"
  fi
done

echo "Uploading Bublock plugins to $REMOTE_DIR ..."
for plugin in "${PLUGINS[@]}"; do
  local_dll="$ROOT/$plugin/bin/Release/net10.0/$plugin.dll"
  sftp_run "put '$local_dll' -o '$REMOTE_DIR/$plugin.dll'"
  shasum -a 256 "$local_dll" | sed "s|$ROOT/||" >> "$BACKUP_DIR/uploaded.sha256"
  echo "  uploaded $plugin.dll"
done

if [[ "$RETIRED_REMOVED" == true ]]; then
  sleep 5
fi

for plugin in "${RETIRED_PLUGINS[@]}"; do
  log_dir="$REMOTE_LOG_DIR/$plugin"
  if sftp_run "cls -d '$log_dir'" >/dev/null 2>&1; then
    if sftp_run "rm -r '$log_dir'" >/dev/null 2>&1; then
      echo "  removed retired log folder $log_dir"
    else
      echo "  warning: could not remove $log_dir (files may still be open); rerun deploy later" >&2
    fi
  fi
done

echo "$STAMP" > "$BACKUP_ROOT/latest"
echo "Deploy OK. Stamp: $STAMP"
if [[ "$BACKUP" == true ]]; then
  echo "Roll back with: $ROOT/scripts/rollback.sh $STAMP"
fi
