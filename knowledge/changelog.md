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

**Gun Game, slice 1 (Stage 13k)**

- `/match_format gungame` on Random mode: every credited kill gives the
  killer a new random hero and top build (`RandomModeService.Reroll`,
  hero-lock safe) and a ladder step; first to the target (default 10)
  wins, the next match auto-starts 10 s later. `/ladder`,
  `/gungame_status`, `/gungame_target`, `/gungame_reroll`. Build clean,
  tests 23 / 99 / 266. Not uploaded yet (UDP game port unreachable).
- Confirmed the deployed Rift Roulette is Theo's latest (`vand0525/bublock`
  is still at `105f1b2`, our fork base).

**Graph**

- `knowledge-graph.py` now adds feature-to-feature dependency edges
  (`feature-uses`, rolled up from file type references, with weights),
  master-plan stage nodes with status and the code each names
  (`touches`), and game-mode nodes from the recipes with the levers they
  use (`mode-uses`, `planned-in`). `indexes.md` lists stages and modes.

**Earlier the same day**

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
- First deploy to the redock server (stamp `20260928T010529Z`, nothing to
  back up). `server-check.sh`: SFTP layout is `/server/game` like Theo's
  host, Deadworks API byte-identical to v0.4.18. All three DLLs hot-loaded
  (`Reload=True`); logs at `Z:\gameserver\server\game\bin\win64\bublock\logs`.
  Warnings: `citadel_crate_disable_early_spawn` missing (expected per
  `patch-day.md`) and `maxplayers` missing as a convar on this host, so the
  13th admin-only slot may not apply (unverified).
- PR #1 on the fork: CI green (build and tests in 28 s).

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
