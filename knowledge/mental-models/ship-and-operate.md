---
id: ship-and-operate
type: mental-model
tags: [ci, cd, deploy, logs, patch-day, server]
related:
  - how-deadworks-mods-work.md
  - ../changelog.md
---

# Ship and operate

## Pipeline

```text
edit ─> scripts/test.sh ─> git push ─> CI (.github/workflows/ci.yml: fetch Deadworks, build, test, keep DLLs)
                                     └> Actions → Deploy → Run workflow (manual = the approval)
                                          └> scripts/deploy.sh --confirm: build, back up server DLLs, upload, keep backup artifact
server hot-reloads the DLLs ─> scripts/pull-logs.sh ─> logs/<Dll>/master-*.log
```

Local deploys use the same script: `scripts/deploy.sh --confirm`. Rollback:
`scripts/rollback.sh <stamp>` (backups in `../server-backups/<stamp>/`, or
the CI run's artifact). Details: `.github/workflows/README.md`,
`scripts/deploy.sh.md`.

## The rule

Nothing reaches the live server without an explicit approval for that
upload (`.rules` §0.2). Deploy is manual in CI for that reason.

## Server layout

A hosted Deadlock server running Deadworks. The panel gives one IP with
several ports: a **game port** (players `connect <ip>:<port>`) and an
**SFTP port** (rclone SFTP, password or public key). Real values live only
in git-ignored `scripts/server.env`.

An unlisted server (`-nomaster`) does not answer server-browser queries on
the game port, and the panel shows "Game offline". Neither means players
can't join: test with a real `connect` and look for the player in
`lobby-*.log`. Match settings (`/match_mode`, `/match_format`) can be set
from the panel's server console as `dw_match_mode` / `dw_match_format`
without anyone connected; they reset on every plugin load.

| SFTP path | Holds |
|---|---|
| `/server/game/bin/win64/managed/plugins/` | our three DLLs |
| `/server/game/bin/win64/managed/DeadworksManaged.Api.dll` | the API version the server runs (the authority for `lib/`) |
| `/server/game/bin/win64/bublock/logs/<Dll>/` | our logs (7-day retention, 10 MB roll) |
| `/server/game/bin/win64/bublock/access.json` | bans, allow list, private mode |
| `/server/game/bin/win64/configs/` | host plugin configs |

## Toolchain on a fresh machine

| Need | Get it |
|---|---|
| .NET 10 SDK | `curl -sSL https://dot.net/v1/dotnet-install.sh \| bash -s -- --channel 10.0` (installs to `~/.dotnet`) |
| Deadworks API | `scripts/fetch-deadworks.sh` (pinned release into `lib/`), or copy from the server |
| lftp | `brew install lftp` (macOS or linuxbrew) or `apt install lftp` |
| SFTP password | `DW_PASSWORD` in `server.env`, macOS keychain, or Linux keyring (`scripts/sftp-password.sh.md`) |

## Patch day

When Deadlock or Deadworks updates, follow `RiftRoulette/reference/patch-day.md`:
baseline before, then `patch-check.py`, update `lib/` (and
`DEADWORKS_VERSION`), rebuild, test, deploy, `dw_selftest_run`. Regenerate
the knowledge base (`scripts/knowledge.sh`) so the API index matches.
