#!/usr/bin/env bash
# Pull Bublock server logs into Bublock/logs/ for Cursor to read.
# Read-only: downloads newer files, never uploads or deletes anything.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOCAL_DIR="$ROOT/logs"

if [[ ! -f "$ROOT/scripts/server.env" ]]; then
  echo "error: copy scripts/server.env.example to scripts/server.env and fill it in." >&2
  exit 1
fi
source "$ROOT/scripts/server.env"
REMOTE_DIR="/server/game/bin/win64/bublock/logs"

if ! command -v lftp >/dev/null 2>&1; then
  echo "error: lftp is not installed (brew install lftp)." >&2
  exit 1
fi

LFTP_PASSWORD="$(security find-generic-password -a "$DW_USER" -s 'deadworks-sftp' -w)"
export LFTP_PASSWORD

if ! lftp --env-password -u "$DW_USER" "sftp://$DW_HOST:$DW_PORT" \
     -e "set cmd:fail-exit yes; cd '$REMOTE_DIR'; bye" >/dev/null 2>&1; then
  echo "No server log folder at $REMOTE_DIR (or server unreachable)."
  echo "Expected until Bublock plugins run on the server (master-plan Stage 12)."
  exit 0
fi

mkdir -p "$LOCAL_DIR"

echo "Pulling $REMOTE_DIR -> $LOCAL_DIR ..."
lftp --env-password -u "$DW_USER" "sftp://$DW_HOST:$DW_PORT" \
  -e "set cmd:fail-exit yes; mirror --only-newer --no-perms --verbose '$REMOTE_DIR' '$LOCAL_DIR'; bye"
echo "Pull OK. Logs are under $LOCAL_DIR/<Dll>/."
