# Sourced by deploy.sh, rollback.sh and pull-logs.sh after server.env: exports
# LFTP_PASSWORD for `lftp --env-password`. See sftp-password.sh.md.

if [[ -n "${DW_PASSWORD:-}" ]]; then
  LFTP_PASSWORD="$DW_PASSWORD"
elif command -v security >/dev/null 2>&1; then
  LFTP_PASSWORD="$(security find-generic-password -a "$DW_USER" -s 'deadworks-sftp' -w)"
elif command -v secret-tool >/dev/null 2>&1; then
  LFTP_PASSWORD="$(secret-tool lookup service deadworks-sftp account "$DW_USER")"
else
  echo "error: no SFTP password: set DW_PASSWORD (scripts/server.env or CI secret)," >&2
  echo "or store it in the keyring (service deadworks-sftp, account DW_USER)." >&2
  exit 1
fi
export LFTP_PASSWORD
