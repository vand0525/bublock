# pull-logs.sh

Mirrors server log files into `Bublock/logs/` and downloads the join access
file (`access.json`: ban list, whitelist, open / private mode, statue
modifier) into `Bublock/server-data/`, so Cursor can read both and there is
a local copy of the lists until they move to a database.

## Usage

```bash
./Bublock/scripts/pull-logs.sh
```

`deploy.sh` also runs it before every upload.

## Behavior

- Logs source: `/server/game/bin/win64/bublock/logs/` on the Deadworks host
  from `scripts/server.env` (same file and keychain entry `deadworks-sftp`
  as `deploy.sh`).
- Logs destination: `Bublock/logs/` (one subfolder per DLL, e.g.
  `logs/RiftRoulette/master-20260926.log`), `lftp mirror --only-newer`:
  new or changed files only. A missing remote folder (or unreachable
  server) prints a note and moves on.
- Data files (`DATA_FILES`, today `access.json`) from
  `/server/game/bin/win64/bublock/` to `Bublock/server-data/<file>`,
  overwriting the local copy (`xfer:clobber on`; lftp refuses to overwrite
  by default). A file missing on the server prints a note.
- Exits 0 unless `server.env`, `lftp` or the keychain item is missing, or a
  transfer fails part way.

## Side effects

- Writes local files under `Bublock/logs/` and `Bublock/server-data/`
  (both git-ignored: `access.json` holds Steam IDs and the repo is public).
- Read-only on the server: no uploads, no deletes (`mirror` without
  `--delete` or `--reverse`, `get`).

## Requirements

- `scripts/server.env` (git-ignored; copy `server.env.example`) with
  `DW_HOST`, `DW_PORT`, `DW_USER`. Missing file: exits 1 with a hint.
- `lftp` installed; keychain item `deadworks-sftp` for account `DW_USER`.
- The password is passed via `LFTP_PASSWORD` / `--env-password`, not on the
  command line.
