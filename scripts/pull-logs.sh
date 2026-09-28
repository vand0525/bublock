#!/usr/bin/env bash
# Pull Bublock server logs into Bublock/logs/ and the join access file
# (ban list + whitelist) into Bublock/server-data/ for Cursor to read.
# Read-only: downloads newer files, never uploads or deletes anything.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOCAL_DIR="$ROOT/logs"
DATA_DIR="$ROOT/server-data"

if [[ ! -f "$ROOT/scripts/server.env" ]]; then
  echo "error: copy scripts/server.env.example to scripts/server.env and fill it in." >&2
  exit 1
fi
source "$ROOT/scripts/server.env"
REMOTE_DIR="/server/game/bin/win64/bublock/logs"
REMOTE_DATA_DIR="/server/game/bin/win64/bublock"
DATA_FILES=(access.json)

if ! command -v lftp >/dev/null 2>&1; then
  echo "error: lftp is not installed (brew install lftp)." >&2
  exit 1
fi

LFTP_PASSWORD="$(security find-generic-password -a "$DW_USER" -s 'deadworks-sftp' -w)"
export LFTP_PASSWORD

sftp_run() {
  lftp --env-password -u "$DW_USER" "sftp://$DW_HOST:$DW_PORT" -e "set cmd:fail-exit yes; $1; bye"
}

if sftp_run "cd '$REMOTE_DIR'" >/dev/null 2>&1; then
  mkdir -p "$LOCAL_DIR"
  echo "Pulling $REMOTE_DIR -> $LOCAL_DIR ..."
  sftp_run "mirror --only-newer --no-perms --verbose '$REMOTE_DIR' '$LOCAL_DIR'"
  echo "Logs OK. Logs are under $LOCAL_DIR/<Dll>/."
else
  echo "No server log folder at $REMOTE_DIR (or server unreachable)."
fi

mkdir -p "$DATA_DIR"
for file in "${DATA_FILES[@]}"; do
  if sftp_run "cls -1 '$REMOTE_DATA_DIR/$file'" >/dev/null 2>&1; then
    sftp_run "set xfer:clobber on; get '$REMOTE_DATA_DIR/$file' -o '$DATA_DIR/$file'"
    echo "Pulled $file -> $DATA_DIR/$file"
  else
    echo "note: no $file on the server (nothing to pull)"
  fi
done
