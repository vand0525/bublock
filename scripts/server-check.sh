#!/usr/bin/env bash
# Read-only look at the Deadworks server over SFTP: install layout, Deadworks API
# version vs lib/, plugins, Bublock logs. Never uploads or deletes. See server-check.sh.md.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

if [[ ! -f "$ROOT/scripts/server.env" ]]; then
  echo "error: copy scripts/server.env.example to scripts/server.env and fill it in." >&2
  exit 1
fi
source "$ROOT/scripts/server.env"
if [[ -z "${DW_USER:-}" ]]; then
  echo "error: DW_USER is empty in scripts/server.env (SFTP username from the hosting panel)." >&2
  exit 1
fi
if ! command -v lftp >/dev/null 2>&1; then
  echo "error: lftp is not installed (brew install lftp)." >&2
  exit 1
fi
source "$ROOT/scripts/sftp-password.sh"

sftp_run() {
  lftp --env-password -u "$DW_USER" "sftp://$DW_HOST:$DW_PORT" -e "set cmd:fail-exit yes; set net:max-retries 1; set net:timeout 15; $1; bye"
}

echo "== Login"
if ! sftp_run "cls -1 /" >/dev/null; then
  echo "FAIL  SFTP login refused or server unreachable (check DW_USER, the password and DW_PORT)." >&2
  exit 1
fi
echo "OK    SFTP login"

echo "== Layout"
WIN64=""
# Theo's host: /server/game; the deadworks.net panel roots its file view at the game folder.
for game in "${DW_REMOTE_GAME:-/server/game}" "/game" ""; do
  candidate="${game%/}/bin/win64"
  if sftp_run "cls -d '$candidate'" >/dev/null 2>&1; then
    WIN64="$candidate"
    break
  fi
done
if [[ -z "$WIN64" ]]; then
  echo "FAIL  no bin/win64 under ${DW_REMOTE_GAME:-/server/game}, /game or /. Top level is:"
  sftp_run "cls -1 /" | sed 's/^/        /'
  exit 1
fi
echo "OK    game folder: $WIN64"
if [[ "$WIN64" != "/server/game/bin/win64" ]]; then
  echo "NOTE  deploy.sh, rollback.sh and pull-logs.sh expect /server/game/bin/win64; set DW_REMOTE_GAME=\"${WIN64%/bin/win64}\" in server.env."
fi

echo "== Deadworks"
if sftp_run "cls -1 '$WIN64/deadworks.exe'" >/dev/null 2>&1; then
  echo "OK    deadworks.exe present"
else
  echo "FAIL  no deadworks.exe: Deadworks is not installed on this server (see server-check.sh.md)."
fi
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
if sftp_run "get '$WIN64/managed/DeadworksManaged.Api.dll' -o '$TMP/DeadworksManaged.Api.dll'" >/dev/null 2>&1; then
  server_sha="$(sha256sum "$TMP/DeadworksManaged.Api.dll" | cut -d' ' -f1)"
  local_dll="${DeadworksLibDir:-$ROOT/lib}/DeadworksManaged.Api.dll"
  if [[ -f "$local_dll" && "$(sha256sum "$local_dll" | cut -d' ' -f1)" == "$server_sha" ]]; then
    echo "OK    server API matches lib/ ($(cat "$(dirname "$local_dll")/.version" 2>/dev/null || echo 'unknown version'))"
  else
    echo "WARN  server DeadworksManaged.Api.dll differs from lib/: build against the server's copy before deploying"
    echo "      (patch-day.md step 3; the server copy is in the managed/ folder)."
  fi
else
  echo "FAIL  no managed/DeadworksManaged.Api.dll"
fi

echo "== Plugins ($WIN64/managed/plugins)"
if sftp_run "cls -1 '$WIN64/managed/plugins'" > "$TMP/plugins" 2>/dev/null; then
  if [[ -s "$TMP/plugins" ]]; then sed 's|.*/||; s/^/        /' "$TMP/plugins"; else echo "        (empty)"; fi
  for plugin in RiftRoulette DevTools CleanSlate; do
    grep -q "/$plugin.dll$\|^$plugin.dll$" "$TMP/plugins" && echo "OK    $plugin.dll deployed" || echo "INFO  $plugin.dll not deployed yet"
  done
else
  echo "INFO  no plugins folder yet (deploy.sh creates nothing; Deadworks makes it on first start)"
fi

echo "== Bublock data ($WIN64/bublock)"
if sftp_run "cls -1 '$WIN64/bublock/logs'" >/dev/null 2>&1; then
  echo "OK    logs folder present (scripts/pull-logs.sh mirrors it)"
else
  echo "INFO  no logs yet (written once the plugins load)"
fi
echo "Check done. Nothing was uploaded or changed."
