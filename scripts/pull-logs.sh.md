# pull-logs.sh

Mirrors server log files into `Bublock/logs/` so Cursor can read them.

## Usage

```bash
./Bublock/scripts/pull-logs.sh
```

## Behavior

- Source: `/server/game/bin/win64/bublock/logs/` on the Deadworks host
  from `scripts/server.env` (same file and keychain entry `deadworks-sftp`
  as `deploy.sh`).
- Destination: `Bublock/logs/` (one subfolder per DLL, e.g.
  `logs/RiftRoulette/master-20260926.log`).
- `lftp mirror --only-newer`: downloads new or changed files only.
- If the remote folder does not exist yet (or the server is unreachable),
  prints a note and exits 0.

## Side effects

- Writes local files under `Bublock/logs/` (git-ignored).
- Read-only on the server: no uploads, no deletes (`mirror` without
  `--delete` or `--reverse`).

## Requirements

- `scripts/server.env` (git-ignored; copy `server.env.example`) with
  `DW_HOST`, `DW_PORT`, `DW_USER`. Missing file: exits 1 with a hint.
- `lftp` installed; keychain item `deadworks-sftp` for account `DW_USER`.
- The password is passed via `LFTP_PASSWORD` / `--env-password`, not on the
  command line.

## Notes

- Allowed before Stage 12 because it only reads. Remote logs exist only once
  Bublock plugins run on the server (Stage 12 push).
