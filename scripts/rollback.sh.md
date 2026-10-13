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
- Restoring a DLL first deletes the one it can't load alongside
  (`conflicts_with` in the script): a backup of a renamed plugin under its
  old name (`RiftRumble.dll`) deletes the new name (`RiftRoulette.dll`), and
  restoring one mode DLL (`RiftRoulette.dll` or `CrystalCrackers.dll`)
  deletes the other. Rolling back the first Crystal Crackers deploy
  therefore puts Rift Roulette back and takes Crystal Crackers off.
- A backup set holding both mode DLLs is refused (exit 1) before anything
  is uploaded.
- A plugin that had no server copy at deploy time has no backup; rollback
  leaves the uploaded Bublock DLL in place (remove it by hand if needed).

## Requirements

Same as `deploy.sh` (`scripts/server.env`, `lftp`, keychain
`deadworks-sftp`, password via `--env-password`).
