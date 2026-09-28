# Bublock — Behavioral Inventory (Stage 5)

Every behavior the archive plugins have today, with the place it will live,
the named operation that owns it, the command(s) that reach it, who may run
it, and the stage that extracts it. This is the checklist for Stages 6–11 and
the parity checklist for Stage 12.

Sources (line numbers refer to these files; Legacy is byte-identical to the
archive copy):

- `RiftRumble/Legacy/RiftRumblePlugin.cs` (deleted in Stage 11) = `archive/RiftRumble/RiftRumblePlugin.cs`
- `DevTools/DevToolsPlugin.cs` (archive plus Stage 3/4 auth and logging)
- `CleanSlate/CleanSlatePlugin.cs` (archive plus Stage 4 logging)

Columns:

- **Owner** — module (`Modules/<Name>`) or plugin class folder (`RiftRoulette/<Feature>`)
- **Op** — service method name the behavior becomes (proposed; final names set in the extraction stage)
- **Command** — in-game `/name` (Deadworks also registers `!name` and `dw_name`)
- **Gate** — `player` (anyone) or `admin` (`AdminAuth.IsAuthorized`)
- **Log** — feature log file the behavior writes to (`<feature>-YYYYMMDD.log`)

---

## Decisions

Parity means the archive's **functionality**, not its command names. The
command set below follows SourceMod / CounterStrikeSharp conventions.

- **Player commands** are short verbs with no prefix: `/pick`, `/unpick`,
  `/picks`, `/heroes`, `/status`, `/commands`.
- **Admin commands** are grouped by feature, feature word first:
  `/rift_start`, `/draft_reset`, `/player_kick`, `/wt_list`, `/mv_tp`,
  `/ent_find`. The prefix is the owning feature: `player_` / `lobby_`
  (Lobby), `draft_`, `rift_`, `session_`, `wt_` (WorldText), `mv_`
  (Movement), `ent_` / `dev_` (DevTools), `cleanup_` (CleanSlate).
- **Every command runs in game as `/name`**; Deadworks also registers
  `!name` and `dw_name`.
- **Old archive names were removed** (Stage 12 decision). During Stages 6–11
  they were hidden aliases (`[Command("<old>", Hidden = true)]` wrappers);
  now only the new names work. The "Archive name" columns below record the
  archive command each one replaced.
- **Targets are slot numbers** (`<slot>`), as in the archive `/kick <slot>`.
- **Help.** Deadworks' built-in `dw_help` is a console command only (no chat
  form); it lists every non-hidden command, admin ones included. Players get
  `/commands` in chat (Stage 12). Every command sets `Description`. Do not
  name a command `help`.
- **Admin gate.** Every admin command checks `AdminAuth`. The archive
  commands `/reset`, `/kick`, `/koth`, `/test` and four DevTools commands
  were ungated; their new names are gated (intentional difference). A null
  caller (server console) is trusted.
- **No name collisions across DLLs.** Separate DLLs register independently,
  so RiftRoulette, DevTools, and CleanSlate names must not overlap.
- **Hero-pool module: not yet.** Draft stays in `RiftRoulette/Draft/`. Revisit
  if a second game mode needs hero pools.

---

## 1. Rift Roulette (Legacy)

### 1.1 Server setup

| Behavior | Source | Owner | Op | Command | Gate | Log | Stage |
|---|---|---|---|---|---|---|---|
| Set convars: `citadel_team_size 6`, `maxplayers 12` (13 since Stage 13f: admin seat), `sv_visiblemaxplayers 12`, `citadel_koth_enabled 0`, `citadel_allow_purchasing_anywhere 1`, `citadel_allow_duplicate_heroes 1` | 123–133 | `RiftRoulette/Lobby` | `ApplyServerConvars` | hook `OnStartupServer`; `/lobby_setup` (new) | admin | lobby | 8 |
| Execute `citadel_koth_warning_time 1`, `citadel_koth_early_warning_time 1`, `citadel_player_override_spawn_time 1` | 129–131 | `RiftRoulette/Lobby` | `ApplyServerConvars` | hook `OnStartupServer` | — | lobby | 8 |
| Draw draft boards next tick after startup | 134 | `RiftRoulette/Draft` (via WorldText) | `RedrawBoards` | hook `OnStartupServer` | — | draft | 6 (WorldText), 9 (Draft) |

### 1.2 Connect, disconnect, spawn, death

| Behavior | Source | Owner | Op | Command | Gate | Log | Stage |
|---|---|---|---|---|---|---|---|
| On full connect: `SelectHero(Skyrunner)`, `ChangeTeam(2, true)`, teleport to draft (no bot check) | 222–232 | `RiftRoulette/Lobby` (uses Movement) | `AdmitPlayer` | hook `OnClientFullConnect` | — | lobby | 8 |
| On disconnect: release draft selection (log + redraw boards if one existed), then remove hero pawn and controller | 234–257 | `RiftRoulette/Lobby` (calls Draft `ReleaseSelection`) | `RemovePlayer` | hook `OnClientDisconnect` | — | lobby | 8 |
| On `player_spawn` (non-bot): teleport to draft next tick | 203–220 | `RiftRoulette/Lobby` (uses Movement) | `ReturnToDraftOnSpawn` | hook `player_spawn` | — | lobby | 8 |
| On `player_death`: diagnostic line (slot, life state, health, position) | 183–201 | `RiftRoulette/Lobby` | `LogDeath` | hook `player_death` | — | players | 8 |

### 1.3 Draft

| Behavior | Source | Owner | Op | Command | Gate | Log | Stage |
|---|---|---|---|---|---|---|---|
| Hero pools: Sapphire = Shiv, Yamato, Mirage, Wraith, Krill, Viper; Amber = Fencer, Drifter, Doorman, Werewolf, PunkGoat, Magician | 13–32 | `RiftRoulette/Draft` | `DraftPools` (data) | — | — | — | 9 |
| Draft state: `SelectedHeroes` set, `PlayerSelections` (Steam ID → hero) | 34–36 | `RiftRoulette/Draft` | `DraftState` | — | — | — | 9 |
| Select a hero: refuse while dead, unknown hero, already selected by caller, hero already drafted, hero not in a pool. On success: record selection, `ChangeTeam` (Sapphire → 3, Amber → 2), `SelectHero`, starting progression next tick, redraw boards, chat confirm | 266–342 | `RiftRoulette/Draft` | `SelectHero` | `/pick <hero>` (archive `/select`) | player | draft | 9 |
| List selected heroes in chat | 344–348 | `RiftRoulette/Draft` | `ListPicks` | `/picks` (archive `/selected`) | player | draft | 9 |
| Unselect: refuse if no selection or dead. Zero gold/AP/level, release, `ChangeTeam(2, true)`, `SelectHero(Skyrunner)`, teleport to draft next tick, redraw boards, chat confirm | 350–400 | `RiftRoulette/Draft` | `UnselectHero` | `/unpick` (archive `/unselect`) | player | draft | 9 |
| Reset draft: clear state; for each **alive** player zero gold/AP/level, team 2, Skyrunner, teleport to draft next tick (dead players skipped and logged); redraw boards | 402–406, 990–1029 | `RiftRoulette/Draft` | `ResetDraft` | `/draft_reset` (archive `/reset`) | **admin (new)** | draft | 9 |
| Caller pawn state (slot, life state, alive, health, max health, position, entity index). Archive printed it to the server console only; new version also replies to the caller | 408–430 | `RiftRoulette/Lobby` | `DescribePlayer` | `/status` (archive `/state`) | player | players | 8 |
| Hero enforcement on `player_hero_changed`: expected hero = selection or Skyrunner. If wrong hero and alive → `SelectHero(expected)`; if dead → skip and log. If right hero and no selection → zero gold/AP/level | 137–181 | `RiftRoulette/Draft` | `EnforceHero` | hook `player_hero_changed` | — | draft | 9 |
| Starting progression: if still selected, set gold to 25,000. **Quirk to preserve:** the log line says "25,000 souls and 24 AP" but only gold is set | 973–988 | `RiftRoulette/Draft` | `GiveStartingProgression` | — (called by `SelectHero`) | — | draft | 9 |
| Release selection helper (used by disconnect and kick) | 242–252, 474–478 | `RiftRoulette/Draft` | `ReleaseSelection` | — | — | draft | 9 |
| `CanChangeHero`: pawn exists and `IsAlive` | 1058–1064 | `RiftRoulette/Draft` | `CanChangeHero` | — | — | — | 9 |

### 1.4 Boards (in-game text)

| Behavior | Source | Owner | Op | Command | Gate | Log | Stage |
|---|---|---|---|---|---|---|---|
| Create one `point_worldtext` (font 64, `worldUnitsPerPx` = scale, RGBA, reorient 0), then teleport it to position + angle | 897–925 | `Modules/WorldText` | `WorldTextService.Create` | `/wt_create` (new) | admin | worldtext | 6 |
| Redraw: remove **every** `point_worldtext` on the map, then create the three boards | 927–958 | `RiftRoulette/Draft` (layout) + WorldText (clear/create) | `RedrawBoards` | `/draft_boards` (new) | admin | draft | 6, 9 |
| Board text: team name, hero list with `(SELECTED)` markers, footer. Archive footer reads `/select <hero>` / `/unselect`; new footer reads `/pick <hero>` / `/unpick` (only visual change to the boards) | 883–895 | `RiftRoulette/Draft` | `DraftBoardText` | — | — | — | 9 |
| Board layout: welcome "RIFT ROULETTE" (white, scale 3, `GreenAngle`, draft + (500, 500, 300)); Sapphire (0,150,255; scale 0.8; angle (0,360,90); draft + (-90, 500, 0)); Amber (255,70,0; scale 0.8; angle (0,180,90); draft + (90, -500, 0)) | 82–121 | `RiftRoulette/Draft` | `DraftBoardLayout` (data) | — | — | — | 9 |

Note: `CreateBoards` removes all `point_worldtext` entities, not only ours.
WorldText must keep that behavior for the Draft redraw until parity is signed
off (Stage 12); id-scoped clearing is a WorldText addition, not a replacement.

### 1.5 Movement

| Behavior | Source | Owner | Op | Command | Gate | Log | Stage |
|---|---|---|---|---|---|---|---|
| Teleport pawn to draft position (0, 0, 1536.0625), zero velocity, then set camera angle (0, 0, 0) | 483–497 | `Modules/Movement` | `MovementService.TeleportTo(location)` | `/mv_tp` (new) | admin | movement | 7 |
| Set client camera angle via `CCitadelUserMsg_SetClientCameraAngles` to one player | 433–450 | `Modules/Movement` | `MovementService.SetViewAngle` | `/mv_angle` (new) | admin | movement | 7 |
| Teleport every player whose selected hero is in a pool to a position + angle | 499–527 | `Modules/Movement` (player filter supplied by Draft) | `MovementService.TeleportPlayers` | `/mv_tp_team` (new; filters by team number, since the module cannot know draft pools) | admin | movement | 7 |
| Named locations: `draft`; rift starts `green_sapphire`, `green_amber`, `yellow_sapphire`, `yellow_amber` (positions + angles at 38–80) | 38–80 | `RiftRoulette` registers into Movement | `MovementService.Register` | `/mv_list` (new) | admin | movement | 7 |
| Debug teleport of caller to draft | 259–263 | `RiftRoulette/Lobby` (Movement op) | `TeleportTo("draft")` | `/mv_tp draft` (archive `/test`, removed) | **admin (new)** | movement | 7 |

### 1.6 Kick

| Behavior | Source | Owner | Op | Command | Gate | Log | Stage |
|---|---|---|---|---|---|---|---|
| Find player by slot (log if missing); log slot + Steam ID; release draft selection and redraw boards; `kickid <slot>` | 452–481 | `RiftRoulette/Lobby` | `KickPlayer` | `/player_kick <slot>` (archive `/kick`) | **admin (new)** | lobby | 8 |

### 1.7 Rift (`/rift_start`, archive `/koth`)

Preserve this sequence exactly (see `.rules` §8).

| Behavior | Source | Owner | Op | Command | Gate | Log | Stage |
|---|---|---|---|---|---|---|---|
| Find `citadel_gamerules`, read `CCitadelGameRulesProxy.m_pGameRules`; abort with a log line if missing or null | 533–553 | `RiftRoulette/Rift` | `ResolveGameRules` | — | — | rift | 10 |
| Schema accessors `m_vNextKothLocation`, `m_timeNextKothSpawnWindowTime`, `m_timeNextKothSpawn`, `m_timeKothGiveUp` | 555–573 | `RiftRoulette/Rift` | `RiftSchema` | — | — | — | 10 |
| Pick side from `_nextRiftIsGreen` (green (7612, -0.000661, 444) / yellow (-7560, 0, 424)); middle rift (0,0,0) defined but unused | 44–56, 575–582 | `RiftRoulette/Rift` | `NextSide` | `/rift_next <green\|yellow>` (new) | admin | rift | 10 |
| Snapshot existing spawner and trooper entity indexes (scoping only; see `.rules` §8 on EntityIndex) | 584–596 | `RiftRoulette/Rift` | `SnapshotRiftEntities` | — | — | rift | 10 |
| Configure: koth off, set location, window 0, spawn 0, koth on | 603–611 | `RiftRoulette/Rift` | `ConfigureNextRift` | — | — | rift | 10 |
| Wait each tick for a new `citadel_item_koth_spawner`; timeout at run 320 → park scheduler, koth off, log, side unchanged | 614–880 | `RiftRoulette/Rift` | `WaitForSpawner` | — | — | rift | 10 |
| On spawn: park scheduler (window/spawn 999999), koth off | 631–636 | `RiftRoulette/Rift` | `ParkScheduler` | — | — | rift | 10 |
| Move Sapphire/Amber players to the side's start locations | 638–670 | `RiftRoulette/Round` (Movement + Draft pools; called by Rift at the same point) | `RoundFlow.MoveTeamsToRift` | — | — | round | 7, 10, 11 |
| Log give-up time; flip side only after a successful spawn; log next side/window/spawn | 672–684 | `RiftRoulette/Rift` | `AlternateSide` | — | — | rift | 10 |
| Watch each tick: new `npc_trooper` → **finished**; `citadel_koth_cashin` appears then disappears → **tie**. Logs cash-in VData name/handle and give-up time on first sight | 686–858 | `RiftRoulette/Rift` | `WatchOutcome` | — | — | rift | 10 |
| End round after 3 s: return **alive** players to draft (dead are left for `player_spawn`), remove troopers not in the snapshot, log count (since the 2026-09-27 playtest fixes: every `npc_trooper`, plus sweeps 5 s and 10 s later). Same for finished and tie | 745–791, 807–855 | `RiftRoulette/Rift` + `RiftRoulette/Round` (return step) | `EndRound` + `RoundFlow.ReturnPlayersToDraft` + `CleanupRiftTroopers` | `/rift_cleanup` (new) | admin | rift | 10, 11 |
| Whole sequence | 529–881 | `RiftRoulette/Round` composing `RiftRoulette/Rift` | `RoundFlow.RunRound` → `RiftService.RunRift` | `/rift_start` (archive `/koth`) | **admin (new)** | rift | 10, 11 |

### 1.8 Unused code and quirks (decide explicitly at extraction)

| Item | Source | Decision |
|---|---|---|
| `LogPlayerLifeStates` (never called) | 1031–1056 | Becomes the body of `/player_list` (Lobby) in Stage 8 |
| `MiddleRiftPosition` (never used) | 54–56 | Keep as a registered constant in Rift; not in rotation |
| Unused `using` lines (`System.ComponentModel`, `X509Certificates`, `Microsoft.VisualBasic`) | 1–5 | Drop when the file is deleted (Stage 11) |
| `[Rift Wars]` log prefix | 533–873 | Replaced by `[RiftRoulette.Rift]` when moved to the logger |
| "24 AP" log text with no AP grant | 985–987 | Preserve behavior; log message states what actually happens |
| `OnClientFullConnect` has no bot check (`player_spawn` does) | 222–232 | Preserve |

---

## 2. DevTools

All stay in `DevTools/`; no extraction stage. Accepted/rejected admin calls
go to `commands-YYYYMMDD.log`. Done in Stage 12: renamed (old names
removed), all gated, results reply to the caller's console with `[DevTools]`
(the archive printed to the server console, partly with `[Rift Wars]`).

| Archive command | New name | Source | Behavior | Gate in archive | Gate now |
|---|---|---|---|---|---|
| `/logpath` | `/dev_logpath` | 40–52 | Print log root, DevTools folder, session id, file-logging state to the caller's console | admin | admin |
| `/entities <filter>` | `/ent_find <filter>` | 55–82 | List entities whose designer/class/name contains the filter | admin | admin |
| `/entity_info <designerName>` | `/ent_info <designerName>` | 84–138 | Dump fields of every entity with that designer name | **none** | admin |
| `/entity_remove <designerName>` | `/ent_remove <designerName>` | 140–170 | Remove every entity with that designer name | **none** | admin (destructive) |
| `/herowatch` | `/dev_herowatch` | 173–186 | Toggle a 0.5 s timer that logs the caller's hero id changes | admin | admin |
| `/snapshot` | `/ent_snapshot` | 188–204 | Remember all entities (index, designer, class, name) | **none** | admin |
| `/compare` | `/ent_diff` | 206–259 | Print entities added/removed since `/ent_snapshot` | **none** | admin |
| hook `OnUnload` | — | 26–30 | Cancel the hero watcher timer | — | — |

The four archive-ungated commands took no caller parameter; they now take a
nullable caller (server-console calls bind `null` and are trusted) and use
`AdminCommand.Authorize`, which replaced the private `Authenticate` helper.

---

## 3. CleanSlate

Stays in `CleanSlate/`; no extraction stage.

| Behavior | Source | Log |
|---|---|---|
| `OnStartupServer`: `citadel_trooper_spawn_enabled 0`, `citadel_npc_spawn_enabled 0`, `citadel_active_lane 0`, `citadel_midboss_initial_spawn_time_override 999999` | 17–25 | master |
| After 2 s, remove `npc_trooper_boss`, `npc_boss_tier2`, `npc_barrack_boss`, `citadel_item_powerup_spawner`, `citadel_herotest_orbspawner` | 27–38, 41–56 | cleanup (one line per entity), master ("Map cleanup complete") |

Never add `info_super_trooper_spawn` to the removal list (crash risk).

New: `/cleanup_run` (admin) re-runs the convars and removals on demand, for
example after a map change without a restart. Done in Stage 12:
`CleanSlateService.ApplyConvars` / `RemoveMapEntities`, startup unchanged.

---

## 4. Full command set

The target command list. **Status:** `archive` = renames an archive command
(the "Archive name" column; the hidden aliases were removed in Stage 12);
`new` = fills a gap. Each command is built and catalogued in its stage.

### 4.1 Player commands (Clean mode, anyone)

| Command | Archive name | Owner | Op | What it does | Status | Stage |
|---|---|---|---|---|---|---|
| `/pick <hero>` | `/select` | Draft | `SelectHero` | Draft a hero (all archive checks and effects) | archive | 9 |
| `/unpick` | `/unselect` | Draft | `UnselectHero` | Give your hero back (archive effects) | archive | 9 |
| `/picks` | `/selected` | Draft | `ListPicks` | List drafted heroes, now with who picked each | archive | 9 |
| `/heroes` | — | Draft | `ListPools` | Both pools with which heroes are still available (Random mode: "heroes are random") | new | 9 |
| `/status` | `/state` | Lobby | `DescribePlayer` | Your slot, team, hero, pick, life state, health, position; replies to you (archive only printed to the server console) | archive | 8 |
| `/commands` | — | Lobby | `CommandList.PlayerCommands` | Chat list of the player commands (`dw_help` is console-only) | new | 12 |
| `/score` | — | GameLoop | `MatchService.DescribeScore` | Match round and score in chat (1v1: best-streak leaderboard) | new | 13a |
| `/stats` | — | Stats | `StatsService.Describe` | Your match K / D / A and both team totals | new | 13c |
| `/bet <sapphire\|amber>` (or type the team name in chat) | — | Betting | `BettingService.TryBet` | Bet all your chips on the next round (own team only while fighting) | new | betting |
| `/chips` | — | Betting | `BettingService.DescribePlayer` | Your chips, open bet, whether betting is open | new | betting |
| `/queue` | — | Duel | `DuelService.JoinQueue` | Join the 1v1 queue (winner stays on), or see your place (1v1 mode only) | new | 13j |
| `/unqueue` | — | Duel | `DuelService.LeaveQueue` | Leave the 1v1 queue (not while fighting) | new | 13j |

### 4.2 Admin commands — Rift Roulette (Debug mode, `AdminAuth`)

| Command | Archive name | Owner | Op | What it does | Status | Stage |
|---|---|---|---|---|---|---|
| `/player_list` | — | Lobby | `ListPlayers` | Every player: slot, name, Steam ID, team, hero, pick, life state, health, position (from unused `LogPlayerLifeStates`) | new | 8 |
| `/player_info <slot>` | — | Lobby | `DescribePlayer` | `/status` for any slot | new | 8 |
| `/player_kick <slot>` | `/kick` | Lobby | `KickPlayer` | Release pick, redraw boards, `kickid` | archive | 8 |
| `/player_team <slot> <sapphire\|amber>` | — | Lobby | `SetTeam` | Move a player to a team (Sapphire 3, Amber 2) | new | 8 |
| `/lobby_setup` | — | Lobby | `ApplyServerConvars` | Re-apply the startup convars and commands | new | 8 |
| `/pause_allow [on\|off]` | — | Lobby | `PauseGuard.Describe` / `SetAllowed` | Show whether players can pause, or turn pausing on / off (off after every load) | new | pause |
| `dw_seat_spec` | — | Lobby | `AdminSeat.Sit` | Admin (caller, else the admin Steam ID) to the spectator seat, outside teams. Console only (`ConsoleOnly`); any time (spectator team + `MakeObserver`). Admins are seated on every connect | new | 13f |
| `/seat_play` | — | Lobby | `AdminSeat.Stand` | Admin (caller, else the admin Steam ID) out of the seat and onto a team (console: `dw_seat_play`, no arguments) | new | 13f |
| `/seat_status` | — | Lobby | `AdminSeat.Describe` | Player slots, admin seat, `maxplayers` | new | 13f |
| `/spec_auto <on\|off>` | — | Lobby | `StreamCam.SetAuto` | Automatic stream camera on or off for the seated admin (default on; console `dw_spec_auto`) | new | stream camera |
| `/spec_status` | — | Lobby | `StreamCam.Describe` | Who is on camera, top-down state, round and whether its top-down is used | new | stream camera |
| `/spec_overview` | — | Lobby | `StreamCam.ShowOverview` | Top-down over the rift now for 10 s (does not use the round's automatic one) | new | stream camera |
| `/player_ban <slot>` | — | Lobby (Access) | `AccessService.Ban`, `PetrifyBanned` | Ban a connected player (Steam ID to `access.json`); statue, kicked in 30 s | new | access |
| `/ban_add <steamid>` | — | Lobby (Access) | `AccessService.Ban`, `PetrifyBanned` | Ban a Steam64 ID; if connected, statue and kicked in 30 s | new | access |
| `/ban_remove <steamid>` | — | Lobby (Access) | `AccessService.Unban` | Unban a Steam64 ID | new | access |
| `/ban_list` | — | Lobby (Access) | `AccessService.DescribeBanned` | List banned IDs with rejoin strikes and lockouts | new | access |
| `/ban_modifier [name\|none]` | — | Lobby (Access) | `AccessService.SetStatueModifier` | Show or set the statue modifier (saved in `access.json`) | new | access |
| `/allow_add <steamid>` | — | Lobby (Access) | `AccessService.Allow` | Whitelist a Steam64 ID for private mode | new | access |
| `/allow_remove <steamid>` | — | Lobby (Access) | `AccessService.Disallow` | Remove from the whitelist (no kick) | new | access |
| `/allow_list` | — | Lobby (Access) | `AccessService.DescribeAllowed` | List whitelisted IDs | new | access |
| `/access_mode [open\|private]` | — | Lobby (Access) | `AccessService.Describe` / `SetPrivate`, `KickDenied` | Show access, or switch open / private (private kicks players without access) | new | access |
| `/session_info` | — | Session | — | RiftRoulette session id, round id, map, log folder, file-logging state | new | 8 |
| `/selftest_run [all]` | — | SelfTest | `SelfTestService.Run` | Check every game dependency after a patch (PASS / WARN / FAIL) | new | patch-day readiness |
| `/selftest_live <slot>` | — | SelfTest | `SelfTestService.Live` | Teleport, restraint, banner and loadout check on one player | new | patch-day readiness |
| `/draft_status` | — | Draft | `DescribeDraft` | Both pools, every pick with player name and slot | new | 9 |
| `/draft_assign <slot> <hero>` | — | Draft | `SelectHero` | Pick a hero for a player (same rules as `/pick`) | new | 9 |
| `/draft_release <slot>` | — | Draft | `UnselectHero` | Unpick for a player (same effects as `/unpick`) | new | 9 |
| `/draft_reset` | `/reset` | Draft | `ResetDraft` | Clear the whole draft (archive effects) | archive | 9 |
| `/draft_boards` | — | Draft | `RedrawBoards` | Redraw the draft boards | new | 9 |
| `/rift_start` | `/koth` | Rift | `RunRift` | Run the known-good rift sequence | archive | 10 |
| `/rift_status` | — | Rift | `DescribeRift` | Next side, phase (idle / waiting for spawn / live / ending), last outcome | new | 10 |
| `/rift_next <green\|yellow>` | — | Rift | `SetNextSide`, `WatchSpot.MoveAllUp` | Force the next side; players and boards move above it (13i) | new | 10 |
| `/rift_cancel` | — | Rift | `CancelRift` | End our round early: stop the wait/watch sequences, park the scheduler, KOTH off, return alive players to the watch spot (13i), remove rift troopers. The rift objective already on the map stays (no known safe way to remove it) | new | 10 |
| `/rift_cleanup` | — | Rift | `CleanupRiftTroopers` | Remove every rift trooper on the map | new | 10 |
| `/spots_list [green\|yellow]` | — | Round | `SpotCheck.Describe` | Every slot's watch, Sapphire and Amber spot | new | per-slot spots |
| `/spots_walk <watch\|sapphire\|amber> [green\|yellow]` | — | Round | `SpotCheck.Walk` | Teleport the admin through each slot spot and log where the pawn lands | new | per-slot spots |
| `/match_start` | — | GameLoop | `MatchService.Start` | Start the continuous match loop (intermission, round, score banner, repeat) | new | 13a |
| `/match_end` | — | GameLoop | `MatchService.End` | Stop the loop, cancel a running round, final score banner, draft reset | new | 13a |
| `/match_auto <on\|off>` | — | GameLoop | `AutoStartService.SetEnabled` / `Check` | Turn match auto-start (2+ players) / auto-end (under 2) on or off | new | 13d |
| `/match_status` | — | GameLoop | `MatchService.DescribeMatch` | Match phase, round, score, ties, auto-start, intermission, rift phase (1v1: king and streak leaderboard) | new | 13a |
| `/match_intermission <seconds>` | — | GameLoop | `MatchService.SetIntermission` | Seconds between rounds (5-120, default 5) | new | 13a |
| `/match_mode <random\|draft\|duel\|1v1>` | — | GameLoop | `MatchService.SetHeroMode` | Hero mode between matches (default random; `1v1` = duel since 13g); lobby reset | new | 13b |
| `/match_format <continuous>` | — | GameLoop | `MatchService.SetFormat` | Match format between matches (only continuous for now) | new | 13b |
| `/match_config` | — | GameLoop | `MatchService.DescribeConfig` | Current mode, format, allowed values, intermission | new | 13b |
| `/random_status` | — | RandomMode | `RandomModeService.Describe` | Teams, heroes, builds, pending swaps per player | new | 13b |
| `/bet_status` | — | Betting | `BettingService.Describe` | Every player's chips and open bet; redraws the betting board | new | betting |
| `/random_reroll` | — | RandomMode | `RandomModeService.PrepareRound` | New random heroes and builds now (Random mode intermission only) | new | 13b |
| `/duel_copy <slot>` | — | Duel | `DuelService.Copy` | Copy a player's exact hero and build for the 1v1 fighters and start (needs 2 queued since 13j) | new | 13g |
| `/duel_clear` | — | Duel | `DuelService.ClearSnapshot` | Drop the 1v1 build, back to free setup | new | 13g |
| `/duel_status` | — | Duel | `DuelService.Describe` | 1v1 build, lock, queue, players | new | 13g |
| `/duel_queue` | — | Duel | `DuelService.DescribeQueue` | 1v1 queue order, fighters, king and streak | new | 13j |
| `/duel_queue_add <slot>` | — | Duel | `DuelService.JoinQueue` | Put a player in the 1v1 queue | new | 13j |
| `/duel_queue_remove <slot>` | — | Duel | `DuelService.LeaveQueue(force)` | Take a player out of the 1v1 queue (not mid-fight) | new | 13j |
| `/stats_board` | — | Stats | `StatsService.RefreshBoards` / `DescribeAll` | Redraw the stats boards and list every player's K / D / A | new | 13c |
| `/stats_reset` | — | Stats | `StatsService.Reset` | Zero the match stats | new | 13c |
| `/balance_status` | — | Balance | `BalanceService.Describe` | Auto-balance on/off, verdict, counters since the last swap | new | 13c |
| `/balance_auto <on\|off>` | — | Balance | `BalanceService.SetEnabled` | Turn automatic balancing on or off | new | 13c |
| `/balance_now` | — | Balance | `RandomModeService.PrepareRound(forceBalance)` | Force a swap for the leading team, then reroll (Random mode intermission only) | new | 13c |

### 4.3 Admin commands — modules (Debug mode, `AdminAuth`)

Game-agnostic: no Rift Roulette names in arguments (teams are team numbers).

| Command | Archive name | Owner | Op | What it does | Status | Stage |
|---|---|---|---|---|---|---|
| `/wt_list` | — | WorldText | `List` | Boards created through WorldText (id, position, text preview) | new | 6 |
| `/wt_create <id> <text>` | — | WorldText | `Create` | Create a board at the caller's position and facing | new | 6 |
| `/wt_update <id> <text>` | — | WorldText | `Update` | Change a board's text | new | 6 |
| `/wt_remove <id>` | — | WorldText | `Remove` | Remove one board | new | 6 |
| `/wt_clear` | — | WorldText | `ClearAll` | Remove every `point_worldtext` (the archive redraw's first step) | new | 6 |
| `/mv_list` | — | Movement | `List` | Named locations with position and angle | new | 7 |
| `/mv_where` | — | Movement | `Where` | Caller's position and view angle (for recording new locations) | new | 7 |
| `/mv_tp <location> [slot]` | `/test` (= `/mv_tp draft`) | Movement | `TeleportTo` | Teleport caller or a slot to a location | archive | 7 |
| `/mv_tp_team <team> <location>` | — | Movement | `TeleportPlayers` | Teleport everyone on a team number | new | 7 |
| `/mv_tp_all <location>` | — | Movement | `TeleportPlayers` | Teleport every player | new | 7 |
| `/mv_angle <pitch> <yaw> <roll> [slot]` | — | Movement | `SetViewAngle` | Set a camera angle | new | 7 |
| `/mv_save <name>` | — | Movement | `Register` | Save the caller's position and angle as a location (until reload) | new | 7 |
| `/mv_remove <name>` | — | Movement | `Unregister` | Remove a location saved with `/mv_save` | new | 7 |
| `/hud_announce <title> [\| description]` | — | Hud | `AnnounceAll` | On-screen banner to every player | new | 13a |
| `/hud_say <message>` | — | Hud | `AnnounceAll` | Admin talks to the server: message as the big banner title, `Server admin` below (console `dw_hud_say`, works while spectating) | new | admin say |
| `/loadout_give <slot> <hero> [build]` | — | Loadout | `LoadoutService.Swap` | Swap a player to a hero and apply stored build 1-3 (0 = random) | new | 13b |
| `/loadout_copy <from> <to>` | — | Loadout | `LoadoutService.Capture` / `SwapSnapshot` | Copy one player's exact hero, items, abilities and level onto another | new | 13g |
| `/loadout_list <hero>` | — | Loadout | `HeroBuildCatalog.BuildsFor` | A hero's stored builds, the 9 items each grants, planned value, optional groups | new | 13b |
| `/loadout_info` | — | Loadout | `HeroBuildCatalog.Default` | Build data date, source, hero count, baseline value, cap, banned items | new | 13b |
| `/restrain <slot>` | — | Restraint | `RestraintService.Restrain` | Silence, disarm and block melee until released | new | 13h |
| `/restrain_release <slot>` | — | Restraint | `RestraintService.Release` | Lift the restraint | new | 13h |
| `/restrain_list` | — | Restraint | `RestraintService.Describe` | Restrained players and their active modifiers | new | 13h |
| `/status_add <slot> <modifier> [seconds]` | — | Restraint | `RestraintService.AddModifier` | Test any game modifier by name | new | 13h |
| `/status_remove <slot> <modifier>` | — | Restraint | `pawn.RemoveModifier` | Remove a game modifier by name | new | 13h |

### 4.4 Admin commands — tool plugins

See §2 (DevTools renames: `/ent_find`, `/ent_info`, `/ent_remove`,
`/ent_snapshot`, `/ent_diff`, `/dev_herowatch`, `/dev_logpath`) and §3
(CleanSlate `/cleanup_run`). All built in Stage 12.

---

## 5. Console-to-logger map

Each Legacy `Console.WriteLine` moves to the logger in the stage that
extracts its behavior. `P` = pass a `PlayerRef`.

| Source | Line today | Log | Level | P |
|---|---|---|---|---|
| 158–164 | Hero enforcement skipped while dead | draft | Debug | yes |
| 192–198 | PLAYER DEATH | players | Debug | yes |
| 246–249 | Disconnected player removed | lobby | Information | yes |
| 279 | Unknown hero | draft | Information | yes |
| 291–293 | Hero already selected | draft | Information | yes |
| 312–314 | Hero not in draft | draft | Information | yes |
| 337–339 | Slot selected hero | draft | Information | yes |
| 365–368 | Blocked unselect while dead | draft | Information | yes |
| 395–397 | Slot unselected hero | draft | Information | yes |
| 415–417, 421–429 | `/state` (now `/status`) output | players | Information | yes |
| 460–462 | Kick: no player in slot | lobby | Warning | no |
| 468–471 | Kicking player | lobby | Information | yes |
| 533 | Forcing Rift | rift | Information | no |
| 540, 551 | gamerules missing / pointer null | rift | Error | no |
| 598–601 | Spawning side | rift | Information | no |
| 624–629 | RIFT SPAWNED | rift | Information | no |
| 668–670 | Players moved to start positions | rift | Information | no |
| 672–674 | KOTH GiveUp | rift | Debug | no |
| 679–684 | KOTH disabled / next side | rift | Information | no |
| 715–733 | CashIn VData / cash-in appeared | rift | Debug | no |
| 740–743 | RIFT TIED | rift | Information | no |
| 748, 809 | Ending (tied) Rift round | rift | Information | no |
| 761–764, 827–830 | RETURN still dead | rift | Debug | yes |
| 786–790, 850–854 | Round ended, troopers removed | rift | Information | no |
| 799–803 | RIFT FINISHED | rift | Information | no |
| 871–874 | Spawn timed out | rift | Warning | no |
| 985–987 | Gave starting progression | draft | Information | yes |
| 1004–1008 | Reset skipped dead player | draft | Debug | yes |
| 1028 | Draft reset | draft | Information | no |
| 1033–1054 | PLAYER STATES (unused) | players | Information | yes |

Round-level lines (rift spawned, round ended) also go to master via
`BublockLog.Master` so the master log tells the game story.

---

## 6. Stage checklists

Tick items when the behavior has moved out of Legacy.

Every stage also sets `Description` on each command it adds. Stages 7–11
registered hidden aliases for archive names they replaced; Stage 12 removed
them all (ticks below are historical).

### Stage 6 — WorldText (done 2026-09-26)
- [x] `WorldTextService.Create` (1.4 row 1)
- [x] Clear-all used by Draft redraw (1.4 row 2, first step)
- [x] `/wt_list`, `/wt_create`, `/wt_update`, `/wt_remove`, `/wt_clear`

### Stage 7 — Movement (done 2026-09-26)
- [x] `TeleportTo`, `SetViewAngle`, `TeleportPlayers` (1.5)
- [x] Named locations registered by Rift Roulette (1.5)
- [x] `/mv_list`, `/mv_where`, `/mv_tp`, `/mv_tp_team`, `/mv_tp_all`, `/mv_angle`, `/mv_save`, `/mv_remove`
- [x] Hidden alias `/test` (admin-gated)

### Stage 8 — Lobby (done 2026-09-26)
- [x] `ApplyServerConvars` (1.1)
- [x] `AdmitPlayer`, `RemovePlayer`, `ReturnToDraftOnSpawn`, `LogDeath` (1.2)
- [x] `/player_kick` + hidden `/kick`, admin-gated (1.6)
- [x] `/status` + hidden `/state` (player)
- [x] `/player_list`, `/player_info`, `/player_team`, `/lobby_setup`, `/session_info`
- [x] Pick data moved to `RiftRoulette/Draft/DraftState` (early part of 1.3 "Draft state") so Lobby can release and show picks

### Stage 9 — Draft (done 2026-09-26)
- [x] Pools, state, `SelectHero`, `UnselectHero`, `ListPicks`, `ResetDraft`, `EnforceHero`, `GiveStartingProgression`, `ReleaseSelection`, `CanChangeHero` (1.3)
- [x] Board text/layout + `RedrawBoards` via WorldText; footer shows `/pick` / `/unpick` (1.4)
- [x] `/pick`, `/unpick`, `/picks`, `/heroes` + hidden `/select`, `/unselect`, `/selected` (player)
- [x] `/draft_reset` + hidden `/reset`; `/draft_status`, `/draft_assign`, `/draft_release`, `/draft_boards` (admin)

- [x] Every 1.7 op, sequence unchanged
- [x] `/rift_start` + hidden `/koth`, admin-gated
- [x] `/rift_status`, `/rift_next`, `/rift_cancel`, `/rift_cleanup`

- [x] `/rift_start` composes Rift ops; lifecycle path Clean, admin path Debug, same ops
- [x] Legacy folder deleted; every row above has a non-Legacy owner

### Stage 12 — Parity harden (command items)
- [x] DevTools renames + gating of the four ungated commands (§2); no aliases
- [x] CleanSlate `/cleanup_run` (§3)
- [x] `dw_help` is console-only (no chat form); added player `/commands`
- [x] Hidden archive aliases: removed (only the new names work)
- [x] In-game parity checklist green after the first push (`parity-review.md`); user moved on to Stage 13a (2026-09-27)
