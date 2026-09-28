---
id: changelog
type: changelog
tags: [history, fork]
related:
  - README.md
  - mental-models/ship-and-operate.md
---

# Changelog (redock fork)

Changes in this fork (`scho0124/bublock_redock`), newest first. Theo's stage
history for the upstream repo stays in `RiftRoulette/reference/master-plan.md`
→ Progress log. Add a dated entry with every change that ships or changes how
we work.

## 2026-09-28

- `scripts/server-check.sh`: read-only SFTP look at a server (login, layout,
  Deadworks install and API version vs `lib/`, plugins, logs). Waiting on
  the SFTP login in `scripts/server.env` to run it against the new server.
- The redock server's role: experimental debug and game-mode planning
  server. Owner's backlog (Grifball, Fat Boy, zombies, bumper cars, lane
  practice, build orders, strategies) added to `game-mode-recipes.md`.
- Server found: the deadworks.net hosting panel (server "GriffBall", US
  East). Deadworks v0.4.18 installed (managed/ file sizes match the release
  we build against), `managed/plugins/` empty, game build 6701 connected to
  Valve's game coordinator. The panel's file root is the game folder, so
  `deploy.sh`, `rollback.sh`, `pull-logs.sh` and `server-check.sh` now take
  `DW_REMOTE_GAME` (default `/server/game`, Theo's host). SFTP user saved;
  the password comes from the panel's SFTP tab (owner-only step).
- Designs for Grifball and Fat Boy / Zombie Escape in `game-mode-recipes.md`,
  mapped to verified and untested levers.

## 2026-09-27

**Tooling: build, CI/CD and server pin**

- Builds outside Theo's `deadworks/` workspace: `scripts/fetch-deadworks.sh`
  downloads the pinned Deadworks release (v0.4.18) into git-ignored `lib/`;
  `update.sh` and `test.sh` use it when `../Directory.Build.props` is absent.
  Verified on Linux with .NET SDK 10.0.401: build clean, 381 tests pass.
- SFTP password lookup moved into `scripts/sftp-password.sh` (`DW_PASSWORD`,
  macOS keychain, or Linux keyring), used by `deploy.sh`, `rollback.sh` and
  `pull-logs.sh`. The macOS keychain path works as before.
- CI (`.github/workflows/ci.yml`): build and test on every push and PR.
  CD (`deploy.yml`): manual, main only, runs `deploy.sh --confirm` from
  `production` environment secrets and keeps the backup as an artifact.
- Server pinned in git-ignored `scripts/server.env`: the game port and the
  SFTP port (rclone SFTP) on the new host. The SFTP host key was pinned in
  `~/.ssh/known_hosts`.

**Admin**

- Added the server owner's Steam ID to `Shared/Auth/AdminAuth.cs` beside
  Theo's. Admins connect into the spectator seat; `dw_seat_play` joins a team.

**Knowledge base**

- `knowledge/`: glossary, four mental models, effects catalog (verified,
  untested, avoid), game-mode recipes, this changelog.
- Generators: `scripts/api-index.cs` (Deadworks API index from reflection
  and XML docs), `scripts/knowledge-graph.py` (graph, tree, indexes),
  `scripts/docs-site.py` (docs webpage); `scripts/knowledge.sh` runs all
  three.
- `CLAUDE.md` loads `.rules` and points agents at the knowledge base.
