# deploy.sh

Uploads the Bublock plugin DLLs to the Deadworks server, backing up the
server's current copies first. The only push path for Bublock plugins.

## Usage

```bash
./Bublock/scripts/deploy.sh --confirm [--no-backup]
```

Without `--confirm` (or with an unknown argument) it prints usage and exits
1 (nothing built or sent). `--no-backup` skips step 2; the stamp folder then
only holds `uploaded.sha256` and `access.json`, and `rollback.sh` has
nothing to restore.

## Steps

1. Runs `update.sh` (Release build of `Bublock.sln`) and checks that
   `RiftRoulette.dll`, `DevTools.dll`, `CleanSlate.dll` exist under each
   project's `bin/Release/net10.0/`.
2. Backup: for each of the three names and each retired name
   (`RETIRED_PLUGINS`, today `RiftRumble`), if it exists in
   `/server/game/bin/win64/managed/plugins/`, downloads it to
   `deadworks/server-backups/<UTC stamp>/` (e.g. `20260927T011500Z`). A
   failed download aborts before any upload.
3. Pull: runs `pull-logs.sh` (server logs into `Bublock/logs/`,
   `access.json` into `Bublock/server-data/`), then copies `access.json`
   into the stamp folder, so every deploy keeps the ban list and whitelist
   as they were. Runs even with `--no-backup`. A failed pull is a warning
   and the deploy goes on.
4. Retired DLLs: deletes each `RETIRED_PLUGINS` DLL still in the plugins
   folder, before uploading. A renamed plugin otherwise keeps loading under
   its old name and every command registers twice.
5. Upload: `put` each Bublock DLL over the same name in the plugins folder,
   appending its SHA-256 to `<stamp>/uploaded.sha256`.
6. Retired logs: if a retired DLL was deleted, waits 5 s for it to unload,
   then removes `/server/game/bin/win64/bublock/logs/<retired>/` if present.
   A failure (files still open) is only a warning; rerunning deploy retries.
7. Writes the stamp to `server-backups/latest` and prints the rollback
   command.

Steps 4 and 6 do nothing once the old files are gone. `RETIRED_PLUGINS`
must stay non-empty (macOS bash 3.2 with `set -u` rejects an empty array);
delete the loop instead of emptying it.

## Side effects

- Replaces the live plugins; Deadworks reloads changed DLLs from the plugins
  folder (the archive deploy used the same direct `put`).
- Writes backups under `deadworks/server-backups/` (workspace root, outside
  the Bublock git repo, so never committed), including `access.json`.
- Updates the local log and `server-data/` copies (`pull-logs.sh`).

## Requirements

- `scripts/server.env` (git-ignored; copy `server.env.example`) sets
  `DW_HOST`, `DW_PORT` and `DW_USER`. Missing file: exits 1 with a hint.
  Server details never go in committed files.
- `lftp`; keychain item `deadworks-sftp` for account `DW_USER`. The
  password goes through `LFTP_PASSWORD` / `--env-password`, never on the
  command line or in output.

## Policy

Run only after the user approves the upload (Stage 12 gate; afterwards per
change). Roll back with `rollback.sh <stamp>`.
