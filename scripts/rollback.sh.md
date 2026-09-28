# rollback.sh

Re-uploads a backup set taken by `deploy.sh`.

## Usage

```bash
./Bublock/scripts/rollback.sh <stamp>
```

`<stamp>` is a folder name under `deadworks/server-backups/` (the latest is
in `server-backups/latest`). Without a stamp it prints usage and exits 1.

## Behavior

- Uploads every `*.dll` in `server-backups/<stamp>/` to
  `/server/game/bin/win64/managed/plugins/` under its own name.
- Errors if the folder has no DLLs.
- A backup of a renamed plugin under its old name (`RiftRumble.dll`) first
  deletes the new name (`RiftRoulette.dll`) from the server, so both never
  load at once (`renamed_to` in the script).
- A plugin that had no server copy at deploy time has no backup; rollback
  leaves the uploaded Bublock DLL in place (remove it by hand if needed).

## Requirements

Same as `deploy.sh` (`scripts/server.env`, `lftp`, password from
`sftp-password.sh`, passed via `--env-password`).

`DW_REMOTE_GAME` in `server.env` overrides the remote game folder
(default `/server/game`; `""` means the SFTP root, as on the deadworks.net
panel). `server-check.sh` prints the right value.
