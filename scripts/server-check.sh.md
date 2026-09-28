# server-check.sh

Read-only health check of the Deadworks server over SFTP. Run it first on a
new server, and before any deploy to a server we haven't looked at.

## Usage

```bash
./scripts/server-check.sh
```

## Checks, in order

1. **Login**: SFTP with `scripts/server.env` and the password from
   `sftp-password.sh`. Refused or unreachable: exits 1.
2. **Layout**: finds `bin/win64` under the game folder: `DW_REMOTE_GAME`
   (default `/server/game`, Theo's host), then `/game`, then `/` (the
   deadworks.net panel shows the game folder as its root). If it is not
   `/server/game`, prints the `DW_REMOTE_GAME` value to put in `server.env`.
3. **Deadworks**: `deadworks.exe` present, and the server's
   `managed/DeadworksManaged.Api.dll` (downloaded to a temp folder, then
   deleted) compared by SHA-256 with `lib/`. A mismatch means rebuilding
   against the server's copy first (`patch-day.md` step 3).
4. **Plugins**: lists `managed/plugins/` and which of RiftRoulette,
   DevTools and CleanSlate are there.
5. **Bublock data**: whether `bublock/logs/` exists yet.

Lines start with `OK`, `WARN`, `FAIL`, `INFO` or `NOTE`.

## Side effects

None on the server (`cls` and `get` only). Locally, a temp folder that is
removed on exit.

## If Deadworks is not installed

The server runs plain Deadlock. Deadworks releases
(`github.com/Deadworks-net/deadworks/releases`) ship `game/bin/win64/deadworks.exe`
and `managed/`, which go into the server's `game/bin/win64/`, and the host
must start `deadworks.exe` instead of the normal server binary. How to
change the start command depends on the hosting panel; do it with the
owner, not from a script.

## Requirements

Same as `deploy.sh`: `scripts/server.env` with `DW_USER` filled in, `lftp`,
the SFTP host key in `~/.ssh/known_hosts`.
