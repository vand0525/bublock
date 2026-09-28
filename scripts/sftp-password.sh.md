# sftp-password.sh

Sourced (not run) by `deploy.sh`, `rollback.sh` and `pull-logs.sh`, after
they source `server.env`. Sets and exports `LFTP_PASSWORD`, which `lftp
--env-password` reads, so the password never appears on a command line or in
output.

## Sources, first match wins

1. `DW_PASSWORD`: CI secret (`.github/workflows/deploy.yml`) or a line in
   git-ignored `scripts/server.env`.
2. macOS keychain: `security find-generic-password -a "$DW_USER" -s
   deadworks-sftp -w` (the original setup).
3. Linux keyring (libsecret): `secret-tool lookup service deadworks-sftp
   account "$DW_USER"`. Store it once with
   `secret-tool store --label='Deadworks SFTP' service deadworks-sftp account <DW_USER>`
   (`apt install libsecret-tools`).

None found: prints a hint and exits 1 (the caller exits, nothing sent).

## Invariants

- Never echo `LFTP_PASSWORD` / `DW_PASSWORD`; never commit them (`server.env`
  is git-ignored; the repo is public).
