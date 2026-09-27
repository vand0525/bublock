# Stage 12 — Parity review (archive vs Bublock)

> Note (2026-09-27): the game mode was renamed Rift Roulette afterwards. This record is kept as written; `RiftRumble/...` Bublock paths below now live under `RiftRoulette/...`.

Side-by-side check of every archive behavior (rows from
`behavior-inventory.md` §1–§3) against where it lives in Bublock. Local
review done 2026-09-26 by reading both sources; the in-game checklist at the
bottom is what turns this green.

Oracles: `archive/RiftRumble/RiftRumblePlugin.cs`,
`archive/DevTools/DevToolsPlugin.cs`, `archive/CleanSlate/CleanSlatePlugin.cs`.
Bublock ships the same three DLL names (`RiftRumble.dll`, `DevTools.dll`,
`CleanSlate.dll`), so an upload replaces the archive plugins one-for-one.

**Status:** `same` = same behavior (code compared); `diff` = intentional
difference listed below; `in-game` = needs the server to confirm.

## Rift Rumble

| Archive behavior | Bublock owner | Status |
|---|---|---|
| Startup convars and commands (team size 6, max players 12, KOTH off, warning times 1, override spawn 1, purchasing anywhere, duplicate heroes) | `Lobby/LobbyService.ApplyServerConvars` | same |
| Boards drawn next tick after startup | `Draft/DraftPlugin.OnStartupServer` → `DraftService.RedrawBoards` | same |
| Full connect: Skyrunner, team 2, teleport to draft | `LobbyService.AdmitPlayer` | same |
| Disconnect: release pick + redraw, remove pawn and controller | `LobbyService.RemovePlayer` | same |
| `player_spawn` (non-bot): teleport to draft next tick | `LobbyPlugin.OnPlayerSpawn` | same |
| `player_death` diagnostic | `LobbyService.LogDeath` (`players` log, Debug) | same (log file instead of console) |
| Hero pools (Sapphire / Amber, six each) | `Draft/DraftPools` | same (unit-tested) |
| `/select <hero>`: all refusals, team change, `SelectHero`, 25,000 gold next tick, redraw, chat | `/pick` → `DraftService.Pick` | same behavior; diff (name) |
| `/selected` | `/picks` → `DraftService.DescribePicks` | diff (also shows who picked) |
| `/unselect`: refusals, zero gold/AP/level, team 2 Skyrunner, draft teleport next tick, redraw | `/unpick` → `DraftService.Unpick` | same behavior; diff (name) |
| `/reset`: clear picks, reset alive players, skip dead, redraw | `/draft_reset` → `DraftService.Reset` | same behavior; diff (name, admin-only) |
| `/state`: caller pawn state | `/status` → `LobbyService.DescribePlayer` | diff (replies to the player, not the server console) |
| Hero enforcement on `player_hero_changed` (never `SelectHero` while dead) | `DraftService.EnforceHero` | same |
| Starting progression (gold only; "24 AP" log text) | `DraftService.GiveStartingProgression` | same behavior; log states gold only |
| Board text and layout | `Draft/DraftBoardText`, `DraftService.RedrawBoards` via `Modules/WorldText` | same except footer (diff) |
| Redraw removes every `point_worldtext` | `WorldTextService.ClearAll` | same |
| Draft / rift-start positions and angles | `Locations/RiftRumbleLocations` via `Modules/Movement` | same (unit-tested) |
| Teleport: position, zero velocity, camera angle message | `MovementService.TeleportTo` | same |
| `/test` (teleport caller to draft) | `/mv_tp draft` | diff (name, admin-only) |
| `/kick <slot>`: release pick, redraw, `kickid` | `/player_kick` → `LobbyService.KickPlayer` | same behavior; diff (name, admin-only) |
| `/koth` rift sequence: gamerules lookup, configure, wait for spawner (320 ticks), park, move teams, flip side after spawn, watch finished / tie, end after 3 s, return alive players, remove rift troopers | `/rift_start` → `Round/RoundFlow.RunRound` → `Rift/RiftService.RunRift` | same order (compared step by step in Stages 10–11); diff (name, admin-only, refuses overlap) |
| Middle rift position defined, unused | `RiftSides.MiddlePosition` | same |
| `LogPlayerLifeStates` (never called) | `/player_list` | new use of unused code |

## DevTools

| Archive behavior | Bublock | Status |
|---|---|---|
| `entities <filter>` (admin) | `/ent_find` | diff (name; output to caller's console) |
| `entity_info` (ungated) | `/ent_info` | diff (name, admin-only, caller's console, `[DevTools]` prefix) |
| `entity_remove` (ungated) | `/ent_remove` | diff (name, admin-only, caller's console, each removal logged) |
| `herowatch` (admin) | `/dev_herowatch` | diff (name; output to the watcher's console and `herowatch` log) |
| `snapshot` / `compare` (ungated) | `/ent_snapshot` / `/ent_diff` | diff (names, admin-only, caller's console) |
| `logpath` (Bublock Stage 4, not archive) | `/dev_logpath` | renamed |

## CleanSlate

| Archive behavior | Bublock | Status |
|---|---|---|
| Startup convars (trooper/NPC spawn off, lane 0, midboss 999999) | `CleanSlateService.ApplyConvars` from `OnStartupServer` | same |
| After 2 s remove lane bosses, powerup and orb spawners | `CleanSlateService.RemoveMapEntities` in `Timer.Once(2.Seconds())` | same (same five names; never `info_super_trooper_spawn`) |
| — | `/cleanup_run` (admin) | new |

## Intentional differences

- **Names.** SourceMod-style names only; every archive name is gone (no
  aliases): `/select` `/unselect` `/selected` `/state` `/reset` `/kick`
  `/koth` `/test`, and the DevTools names.
- **Gating.** `/draft_reset`, `/player_kick`, `/rift_start`, `/mv_tp` and four
  DevTools commands were open to anyone in the archive; now admin-only.
- **Replies.** `/status` replies to the player; admin and DevTools results go
  to the caller's console (server console for `dw_` from the server).
- **Board footer** reads `/pick <hero>` / `/unpick`.
- **`/picks`** shows who picked each hero.
- **Overlap.** `/rift_start` refuses while a rift runs (the archive timed out
  after 320 ticks instead).
- **Logging.** Console diagnostics became structured file logs under
  `bublock/logs/<Dll>/`.
- **New commands** (gap fills): `/heroes`, `/commands`, `/player_list`,
  `/player_info`, `/player_team`, `/lobby_setup`, `/session_info`,
  `/draft_status`, `/draft_assign`, `/draft_release`, `/draft_boards`,
  `/rift_status`, `/rift_next`, `/rift_cancel`, `/rift_cleanup`, `/wt_*`,
  `/mv_*`, `/cleanup_run`.

No unintentional gaps found in the local review.

## Coverage

- Every Bublock source file has a sibling `.md` (checked by script); every
  component has a `FEATURE.md` (Shared, Modules/WorldText, Modules/Movement,
  RiftRumble and its Lobby / Draft / Rift / Round / Session / Locations,
  DevTools, CleanSlate, Tests). Subfolders of `Shared/` and `Tests/` are
  covered by their parent's `FEATURE.md`.
- Catalogs: `user-commands.md`, `admin-commands.md` match the `[Command]`
  attributes in source.

## In-game checklist (after upload)

Run as an admin unless noted. Tick when it behaves as described.

**Logging and tooling**
- [x] Upload 2026-09-27 01:18 UTC: all three DLLs hot-reloaded
      (`Loaded Reload=True`), then loaded fresh with `Server startup
      Map=dl_midtown` at 01:18:56.
- [x] `scripts/pull-logs.sh` mirrors `RiftRumble/`, `DevTools/`, `CleanSlate/`
      folders into `Bublock/logs/`, each with `master-YYYYMMDD.log`.
- [ ] `dw_dev_logpath` prints log root
      `Z:\gameserver\server\game\bin\win64\bublock\logs` (the Windows path
      of SFTP `/server/game/bin/win64/bublock/logs`, as logged by
      `Session started LogFolder=`) and file logging `on`.

**Startup and lobby**
- [ ] Map has no lane troopers, lane bosses, or powerup spawners; master log
      in `CleanSlate/` says `Map cleanup complete`. Not logged 25 s after the
      01:18:56 startup with nobody connected (likely no ticks while the server
      is empty; archive used the same 2 s timer). Check after joining; run
      `/cleanup_run` if the map still has bosses or spawners.
- [ ] Joining puts you on team 2 as Skyrunner at the draft area; the three
      draft boards are visible.
- [ ] `/status` (as a player) replies in chat with your slot, team, pick, hero.
- [ ] `/commands` (as a player) lists `/commands`, `/heroes`, `/pick`,
      `/picks`, `/status`, `/unpick`, then `Full list: dw_help in console`.

**Draft**
- [ ] `/heroes` shows both pools.
- [ ] `/pick shiv` moves you to Sapphire as Shiv, gold becomes 25,000, board
      marks Shiv `(SELECTED)`; a second player's `/pick shiv` is refused.
- [ ] `/picks` lists `Shiv (<you>, slot N)`.
- [ ] Changing hero in the hero menu is reverted to your pick while alive.
- [ ] `/unpick` resets you to Skyrunner, team 2, at the draft area; board
      updates.
- [ ] `/draft_reset` clears every pick and returns alive players to the lobby.

**Rift**
- [ ] With picks on both teams, `/rift_start` replies `Rift starting on GREEN`;
      the rift spawns green, teams teleport to their green starts.
- [ ] Capturing ends the round (finished): 3 s later alive players return to
      draft and rift troopers are removed; `/rift_status` shows next YELLOW.
- [ ] Second `/rift_start` spawns yellow; letting the cash-in expire ends it
      as tied.
- [ ] `/rift_start` while a rift runs is refused; `/rift_cancel` stops it and
      returns players.
- [ ] Dead players at round end return to draft when they respawn.

**Admin tools**
- [ ] `/player_kick <slot>` kicks and releases that player's pick.
- [ ] `/cleanup_run` replies with the removed count (0 on a clean map).
- [ ] `/ent_find trooper`, `/ent_snapshot`, `/ent_diff` print to your console.
- [ ] A non-admin running `/rift_start` or `/ent_remove` gets "You are not
      allowed to use this command." and nothing happens.
- [ ] Old names (`/select`, `/koth`, `/reset`, `/entities`) do nothing.

Rollback if anything is badly wrong: `Bublock/scripts/rollback.sh <stamp>`.
