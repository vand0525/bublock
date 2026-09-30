# Bublock — Behavioral Inventory

Every behavior of the archive plugins, with where it lives now, the named
operation that owns it, the command(s) that reach it, who may run it, and the
log it writes to. The side-by-side check against the archive is
`parity-review.md`.

Sources (line numbers refer to these files):

- `archive/RiftRumble/RiftRumblePlugin.cs`
- `DevTools/DevToolsPlugin.cs` (archive plus shared auth and logging)
- `CleanSlate/CleanSlatePlugin.cs` (archive plus logging)

Columns:

- **Owner** — module (`Modules/<Name>`) or plugin class folder (`RiftRoulette/<Feature>`)
- **Op** — service method that owns the behavior
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
- **Archive names do not work**; only the names below exist. The "Archive
  name" columns record the archive command each one maps to.
- **Targets are slot numbers** (`<slot>`).
- **Help.** Deadworks' built-in `dw_help` is a console command only (no chat
  form); it lists every non-hidden command, admin ones included. Players get
  `/commands` in chat. Every command sets `Description`. Do not name a
  command `help`.
- **Admin gate.** Every admin command checks `AdminAuth`. The archive
  commands `/reset`, `/kick`, `/koth`, `/test` and four DevTools commands
  were ungated; their current names are gated. A null caller (server
  console) is trusted.
- **No name collisions across DLLs.** Separate DLLs register independently,
  so RiftRoulette, DevTools, and CleanSlate names must not overlap.
- **Hero pools** live in `RiftRoulette/Draft/` (no hero-pool module).

---

## 1. Rift Roulette (archive `RiftRumble`)

### 1.1 Server setup

| Behavior | Source | Owner | Op | Command | Gate | Log |
|---|---|---|---|---|---|---|
| Set convars: `citadel_team_size 6`, `maxplayers 12` (now 13: admin seat), `sv_visiblemaxplayers 12`, `citadel_koth_enabled 0`, `citadel_allow_purchasing_anywhere 1`, `citadel_allow_duplicate_heroes 1` (now also `citadel_hero_demo_unlock_flex_slots 1` and `FlexSlots.UnlockAll`: `CCitadelTeam.m_nFlexSlotsUnlocked = 15` on both team entities; `citadel_voice_all_talk 1`) | 123–133 | `RiftRoulette/Lobby` | `ApplyServerConvars` | hook `OnStartupServer`; `/lobby_setup` (new) | admin | lobby |
| Execute `citadel_koth_warning_time 1`, `citadel_koth_early_warning_time 1`, `citadel_player_override_spawn_time 1` | 129–131 | `RiftRoulette/Lobby` | `ApplyServerConvars` | hook `OnStartupServer` | — | lobby |
| Draw draft boards next tick after startup | 134 | `RiftRoulette/Draft` (via WorldText) | `RedrawBoards` | hook `OnStartupServer` | — | draft |

### 1.2 Connect, disconnect, spawn, death

| Behavior | Source | Owner | Op | Command | Gate | Log |
|---|---|---|---|---|---|---|
| On full connect: `SelectHero(LobbyHero)`, `ChangeTeam(2, true)`, teleport to draft (no bot check) | 222–232 | `RiftRoulette/Lobby` (uses Movement) | `AdmitPlayer` | hook `OnClientFullConnect` | — | lobby |
| On disconnect: release draft selection (log + redraw boards if one existed), then remove hero pawn and controller | 234–257 | `RiftRoulette/Lobby` (calls Draft `ReleaseSelection`) | `RemovePlayer` | hook `OnClientDisconnect` | — | lobby |
| On `player_spawn` (non-bot): teleport to draft next tick | 203–220 | `RiftRoulette/Lobby` (uses Movement) | `ReturnToDraftOnSpawn` | hook `player_spawn` | — | lobby |
| On `player_death`: diagnostic line (slot, life state, health, position) | 183–201 | `RiftRoulette/Lobby` | `LogDeath` | hook `player_death` | — | players |

### 1.3 Draft

| Behavior | Source | Owner | Op | Command | Gate | Log |
|---|---|---|---|---|---|---|
| Hero pools: Sapphire = Shiv, Yamato, Mirage, Wraith, Krill, Viper; Amber = Fencer, Drifter, Doorman, Werewolf, PunkGoat, Magician | 13–32 | `RiftRoulette/Draft` | `DraftPools` (data) | — | — | — |
| Draft state: `SelectedHeroes` set, `PlayerSelections` (Steam ID → hero) | 34–36 | `RiftRoulette/Draft` | `DraftState` | — | — | — |
| Select a hero: refuse while dead, unknown hero, already selected by caller, hero already drafted, hero not in a pool. On success: record selection, `ChangeTeam` (Sapphire → 3, Amber → 2), `SelectHero`, starting progression next tick, redraw boards, chat confirm | 266–342 | `RiftRoulette/Draft` | `SelectHero` | `/pick <hero>` (archive `/select`) | player | draft |
| List selected heroes in chat | 344–348 | `RiftRoulette/Draft` | `ListPicks` | `/picks` (archive `/selected`) | player | draft |
| Unselect: refuse if no selection or dead. Zero gold/AP/level, release, `ChangeTeam(2, true)`, `SelectHero(LobbyHero)`, teleport to draft next tick, redraw boards, chat confirm | 350–400 | `RiftRoulette/Draft` | `UnselectHero` | `/unpick` (archive `/unselect`) | player | draft |
| Reset draft: clear state; for each **alive** player zero gold/AP/level, team 2, LobbyHero, teleport to draft next tick (dead players skipped and logged); redraw boards | 402–406, 990–1029 | `RiftRoulette/Draft` | `ResetDraft` | `/draft_reset` (archive `/reset`) | **admin** (archive: anyone) | draft |
| Caller pawn state (slot, life state, alive, health, max health, position, entity index). Replies to the caller and logs it (the archive printed it to the server console only) | 408–430 | `RiftRoulette/Lobby` | `DescribePlayer` | `/status` (archive `/state`) | player | players |
| Hero enforcement on `player_hero_changed`: expected hero = selection or LobbyHero. If wrong hero and alive → `SelectHero(expected)`; if dead → skip and log. If right hero and no selection → zero gold/AP/level | 137–181 | `RiftRoulette/Draft` | `EnforceHero` | hook `player_hero_changed` | — | draft |
| Starting progression: if still selected, set gold to 25,000. **Quirk to preserve:** the archive log line says "25,000 souls and 24 AP" but only gold is set | 973–988 | `RiftRoulette/Draft` | `GiveStartingProgression` | — (called by `SelectHero`) | — | draft |
| Release selection helper (used by disconnect and kick) | 242–252, 474–478 | `RiftRoulette/Draft` | `ReleaseSelection` | — | — | draft |
| `CanChangeHero`: pawn exists and `IsAlive` | 1058–1064 | `RiftRoulette/Draft` | `CanChangeHero` | — | — | — |

### 1.4 Boards (in-game text)

| Behavior | Source | Owner | Op | Command | Gate | Log |
|---|---|---|---|---|---|---|
| Create one `point_worldtext` (font 64, `worldUnitsPerPx` = scale, RGBA, reorient 0), then teleport it to position + angle | 897–925 | `Modules/WorldText` | `WorldTextService.Create` | `/wt_create` (new) | admin | worldtext |
| Redraw: remove **every** `point_worldtext` on the map, then create the three boards | 927–958 | `RiftRoulette/Draft` (layout) + WorldText (clear/create) | `RedrawBoards` | `/draft_boards` (new) | admin | draft |
| Board text: team name, hero list with `(SELECTED)` markers, footer `/pick <hero>` / `/unpick` (archive: `/select <hero>` / `/unselect`) | 883–895 | `RiftRoulette/Draft` | `DraftBoardText` | — | — | — |
| Board layout: welcome "RIFT ROULETTE" (white, scale 3, `GreenAngle`, draft + (500, 500, 300)); Sapphire (0,150,255; scale 0.8; angle (0,360,90); draft + (-90, 500, 0)); Amber (255,70,0; scale 0.8; angle (0,180,90); draft + (90, -500, 0)) | 82–121 | `RiftRoulette/Draft` | `DraftBoardLayout` (data) | — | — | — |

Note: `CreateBoards` removes all `point_worldtext` entities, not only ours.
The Draft redraw keeps that behavior (`WorldTextService.ClearAll`); id-scoped
clearing is a separate WorldText operation.

### 1.5 Movement

| Behavior | Source | Owner | Op | Command | Gate | Log |
|---|---|---|---|---|---|---|
| Teleport pawn to draft position (0, 0, 1536.0625), zero velocity, then set camera angle (0, 0, 0) | 483–497 | `Modules/Movement` | `MovementService.TeleportTo(location)` | `/mv_tp` (new) | admin | movement |
| Set client camera angle via `CCitadelUserMsg_SetClientCameraAngles` to one player | 433–450 | `Modules/Movement` | `MovementService.SetViewAngle` | `/mv_angle` (new) | admin | movement |
| Teleport every player whose selected hero is in a pool to a position + angle | 499–527 | `Modules/Movement` (player filter supplied by Draft) | `MovementService.TeleportPlayers` | `/mv_tp_team` (new; filters by team number, since the module cannot know draft pools) | admin | movement |
| Named locations: `draft`; rift starts `green_sapphire`, `green_amber`, `yellow_sapphire`, `yellow_amber` (positions + angles at 38–80) | 38–80 | `RiftRoulette` registers into Movement | `MovementService.Register` | `/mv_list` (new) | admin | movement |
| Debug teleport of caller to draft | 259–263 | `RiftRoulette/Lobby` (Movement op) | `TeleportTo("draft")` | `/mv_tp draft` (archive `/test`) | **admin** (archive: anyone) | movement |

### 1.6 Kick

| Behavior | Source | Owner | Op | Command | Gate | Log |
|---|---|---|---|---|---|---|
| Find player by slot (log if missing); log slot + Steam ID; release draft selection and redraw boards; `kickid <slot>` | 452–481 | `RiftRoulette/Lobby` | `KickPlayer` | `/player_kick <slot>` (archive `/kick`) | **admin** (archive: anyone) | lobby |

### 1.7 Rift (`/rift_start`, archive `/koth`)

Preserve this sequence exactly (see `.rules` §8).

| Behavior | Source | Owner | Op | Command | Gate | Log |
|---|---|---|---|---|---|---|
| Find `citadel_gamerules`, read `CCitadelGameRulesProxy.m_pGameRules`; abort with a log line if missing or null | 533–553 | `RiftRoulette/Rift` | `ResolveGameRules` | — | — | rift |
| Schema accessors `m_vNextKothLocation`, `m_timeNextKothSpawnWindowTime`, `m_timeNextKothSpawn`, `m_timeKothGiveUp` | 555–573 | `RiftRoulette/Rift` | `RiftSchema` | — | — | — |
| Pick side from rotation (green (7612, -0.000661, 444) / yellow (-7560, 0, 424) / center (0, 0, 448)); mid on by default (`/rift_mid`) | 44–56, 575–582 | `RiftRoulette/Rift` | `NextSide`, `NextInRotation` | `/rift_next <green\|yellow\|center>`, `/rift_mid` | admin | rift |
| Snapshot existing spawner and trooper entity indexes (scoping only; see `.rules` §8 on EntityIndex) | 584–596 | `RiftRoulette/Rift` | `SnapshotRiftEntities` | — | — | rift |
| Configure: koth off, set location, window 0, spawn 0, koth on | 603–611 | `RiftRoulette/Rift` | `ConfigureNextRift` | — | — | rift |
| Wait each tick for a new `citadel_item_koth_spawner`; timeout at run 320 → park scheduler, koth off, log, side unchanged | 614–880 | `RiftRoulette/Rift` | `WaitForSpawner` | — | — | rift |
| On spawn: park scheduler (window/spawn 999999), koth off | 631–636 | `RiftRoulette/Rift` | `ParkScheduler` | — | — | rift |
| Move Sapphire/Amber players to the side's start locations | 638–670 | `RiftRoulette/Round` (Movement + Draft pools; called by Rift at the same point) | `RoundFlow.MoveTeamsToRift` | — | — | round |
| Log give-up time; flip side only after a successful spawn; log next side/window/spawn | 672–684 | `RiftRoulette/Rift` | `AlternateSide` | — | — | rift |
| Watch each tick: new `npc_trooper` → **finished**; `citadel_koth_cashin` appears then disappears → **tie**. Logs cash-in VData name/handle and give-up time on first sight | 686–858 | `RiftRoulette/Rift` | `WatchOutcome` | — | — | rift |
| End round after 3 s: return **alive** players to draft (dead are left for `player_spawn`), remove every `npc_trooper` (archive: only troopers not in the snapshot) and sweep again 5 s and 10 s later, log count. Same for finished and tie | 745–791, 807–855 | `RiftRoulette/Rift` + `RiftRoulette/Round` (return step) | `EndRound` + `RoundFlow.ReturnPlayersToDraft` + `CleanupRiftTroopers` | `/rift_cleanup` (new) | admin | rift |
| Whole sequence | 529–881 | `RiftRoulette/Round` composing `RiftRoulette/Rift` | `RoundFlow.RunRound` → `RiftService.RunRift` | `/rift_start` (archive `/koth`) | **admin** (archive: anyone) | rift |

### 1.8 Unused code and quirks

| Item | Source | Decision |
|---|---|---|
| `LogPlayerLifeStates` (never called) | 1031–1056 | Body of `/player_list` (Lobby) |
| `MiddlePosition` (center spawn, z 448) | — | In rotation when `MiddleEnabled`; `/rift_mid` toggles |
| Unused `using` lines (`System.ComponentModel`, `X509Certificates`, `Microsoft.VisualBasic`) | 1–5 | Not carried over |
| `[Rift Wars]` log prefix | 533–873 | Logs use `[RiftRoulette.Rift]` |
| "24 AP" log text with no AP grant | 985–987 | Behavior kept (gold only); the log message states what actually happens |
| `OnClientFullConnect` has no bot check (`player_spawn` does) | 222–232 | Kept |

---

## 2. DevTools

Lives in `DevTools/`. Accepted and rejected admin calls go to
`commands-YYYYMMDD.log`. Every command is admin-gated and replies to the
caller's console with `[DevTools]` (the archive printed to the server
console, partly with `[Rift Wars]`).

| Archive command | Command | Source | Behavior | Archive gate | Gate |
|---|---|---|---|---|---|
| `/logpath` | `/dev_logpath` | 40–52 | Print log root, DevTools folder, session id, file-logging state to the caller's console | admin | admin |
| `/entities <filter>` | `/ent_find <filter>` | 55–82 | List entities whose designer/class/name contains the filter | admin | admin |
| `/entity_info <designerName>` | `/ent_info <designerName>` | 84–138 | Dump fields of every entity with that designer name | **none** | admin |
| `/entity_remove <designerName>` | `/ent_remove <designerName>` | 140–170 | Remove every entity with that designer name | **none** | admin (destructive) |
| `/herowatch` | `/dev_herowatch` | 173–186 | Toggle a 0.5 s timer that logs the caller's hero id changes | admin | admin |
| `/snapshot` | `/ent_snapshot` | 188–204 | Remember all entities (index, designer, class, name) | **none** | admin |
| `/compare` | `/ent_diff` | 206–259 | Print entities added/removed since `/ent_snapshot` | **none** | admin |
| hook `OnUnload` | — | 26–30 | Cancel the hero watcher timer | — | — |

Every command takes a nullable caller (server-console calls bind `null` and
are trusted) and checks `AdminCommand.Authorize`.

---

## 3. CleanSlate

Lives in `CleanSlate/`.

| Behavior | Source | Log |
|---|---|---|
| `OnStartupServer`: `citadel_trooper_spawn_enabled 0`, `citadel_npc_spawn_enabled 0`, `citadel_active_lane 0`, `citadel_midboss_initial_spawn_time_override 999999` | 17–25 | master |
| After 2 s, remove `npc_trooper_boss`, `npc_boss_tier2`, `npc_barrack_boss`, `citadel_item_powerup_spawner`, `citadel_herotest_orbspawner` | 27–38, 41–56 | cleanup (one line per entity), master ("Map cleanup complete") |

Never add `info_super_trooper_spawn` to the removal list (crash risk).

`/cleanup_run` (admin, no archive equivalent) re-runs the convars and
removals on demand (`CleanSlateService.ApplyConvars` / `RemoveMapEntities`,
the same ops startup uses), for example after a map change without a
restart.

---

## 4. Full command set

**Status:** `archive` = renames an archive command (the "Archive name"
column); `new` = no archive equivalent.

### 4.1 Player commands (Clean mode, anyone)

| Command | Archive name | Owner | Op | What it does | Status |
|---|---|---|---|---|---|
| `/pick <hero>` | `/select` | Draft | `SelectHero` | Draft a hero (all archive checks and effects) | archive |
| `/unpick` | `/unselect` | Draft | `UnselectHero` | Give your hero back (archive effects) | archive |
| `/picks` | `/selected` | Draft | `ListPicks` | List drafted heroes and who picked each | archive |
| `/heroes` | — | Draft | `ListPools` | Both pools with which heroes are still available (Random mode: "heroes are random") | new |
| `/status` | `/state` | Lobby | `DescribePlayer` | Your slot, team, hero, pick, life state, health, position; replies to you | archive |
| `/commands` | — | Lobby | `CommandList.PlayerCommands` | Chat list of the player commands (`dw_help` is console-only) | new |
| `/score` | — | GameLoop | `MatchService.DescribeScore` | Match round and score in chat (1v1: best-streak leaderboard) | new |
| `/stats` | — | Stats | `StatsService.Describe` | Your match K / D / A and both team totals | new |
| `/bet <sapphire\|amber>` (or type the team name in chat) | — | Betting | `BettingService.TryBet` | Bet all your souls on the next round (own team only while fighting) | new |
| `/souls` | — | Betting | `BettingService.DescribePlayer` | Your souls, open bet, whether betting is open, your hero reservation | new |
| `/reserve [hero]` | — | Random | `RandomModeService.Reserve` | Spend 1,000 souls to play a hero in your next 3 fighting rounds; a waiting line per hero (Random mode match only) | new |
| `/heroban [hero]` | — | Random | `RandomModeService.Ban` | Spend 1,000 souls to ban a hero from the next draw for both teams; one per team per round, revealed at round start (Random mode match only) | new |
| `/queue` | — | Duel | `DuelService.JoinQueue` | Join the 1v1 queue (winner stays on), or see your place (1v1 mode only) | new |
| `/unqueue` | — | Duel | `DuelService.LeaveQueue` | Leave the 1v1 queue (not while fighting) | new |

### 4.2 Admin commands — Rift Roulette (Debug mode, `AdminAuth`)

| Command | Archive name | Owner | Op | What it does | Status |
|---|---|---|---|---|---|
| `/player_list` | — | Lobby | `ListPlayers` | Every player: slot, name, Steam ID, team, hero, pick, life state, health, position (from unused `LogPlayerLifeStates`) | new |
| `/player_info <slot>` | — | Lobby | `DescribePlayer` | `/status` for any slot | new |
| `/player_kick <slot>` | `/kick` | Lobby | `KickPlayer` | Release pick, redraw boards, `kickid` | archive |
| `/player_team <slot> <sapphire\|amber>` | — | Lobby | `SetTeam` | Move a player to a team (Sapphire 3, Amber 2) | new |
| `/lobby_setup` | — | Lobby | `ApplyServerConvars` | Re-apply the startup convars and commands | new |
| `/lobby_flex` | — | Lobby | `FlexSlots.UnlockAll` / `Describe` | Open every flex slot on both teams and show each team's flags | new |
| `/pause_allow [on\|off]` | — | Lobby | `PauseGuard.Describe` / `SetAllowed` | Show whether players can pause, or turn pausing on / off (every load sets it from private mode) | new |
| `dw_seat_spec` | — | Lobby | `AdminSeat.Sit` | Admin (caller, else the admin Steam ID) to the spectator seat, outside teams. Console only (`ConsoleOnly`); any time (spectator team + `MakeObserver`). Admins are seated on every connect | new |
| `/seat_play` | — | Lobby | `AdminSeat.Stand` | Admin (caller, else the admin Steam ID) out of the seat and onto a team (console: `dw_seat_play`, no arguments) | new |
| `/seat_roam` | — | Lobby | `AdminSeat.RoamNow` | Seated admin roams now as invisible Abrams in front of the welcome sign, or is put back there if already roaming; allowed while players are connected (console: `dw_seat_roam`) | new |
| `/restart_status` | — | Lobby | `AutoRestartService.Describe` | Auto restart on / off, map uptime, stuck and in-progress joins, join budget | new |
| `/restart_now` | — | Lobby | `AutoRestartService.Restart` | Reload the map now (`changelevel`); every connected client reconnects by itself | new |
| `/restart_auto <on\|off>` | — | Lobby | `AutoRestartService.SetEnabled` | Automatic map reload (stuck join or 3 h up, nobody playing) on or off until the next load | new |
| `/restart_budget [n]` | — | Lobby | `MapRefreshService.Describe` / `SetBudget` | Join budget refresh: show, or set fighter-rounds before the round-end map reload (default 160, 0 = off) until the next load | new |
| `/seat_status` | — | Lobby | `AdminSeat.Describe` | Player slots, admin seat, `maxplayers` | new |
| `/spec_auto <on\|off>` | — | Lobby | `StreamCam.SetAuto` | Automatic stream camera on or off for the seated admin (default on; console `dw_spec_auto`) | new |
| `/spec_status` | — | Lobby | `StreamCam.Describe` | Who is on camera, fly cam, view angle read, saved framing per side | new |
| `/spec_reset` | — | Lobby | `StreamCam.ResetFraming` | Forget the saved framing; parks go back to the top-down default | new |
| `/player_ban <slot>` | — | Lobby (Access) | `AccessService.Ban`, `PetrifyBanned` | Ban a connected player (Steam ID to `access.json`); statue, kicked in 30 s | new |
| `/ban_add <steamid>` | — | Lobby (Access) | `AccessService.Ban`, `PetrifyBanned` | Ban a Steam64 ID; if connected, statue and kicked in 30 s | new |
| `/ban_remove <steamid>` | — | Lobby (Access) | `AccessService.Unban` | Unban a Steam64 ID | new |
| `/ban_list` | — | Lobby (Access) | `AccessService.DescribeBanned` | List banned IDs with rejoin strikes and lockouts | new |
| `/ban_modifier [name\|none]` | — | Lobby (Access) | `AccessService.SetStatueModifier` | Show or set the statue modifier (saved in `access.json`) | new |
| `/allow_add <steamid>` | — | Lobby (Access) | `AccessService.Allow` | Whitelist a Steam64 ID for private mode | new |
| `/allow_remove <steamid>` | — | Lobby (Access) | `AccessService.Disallow` | Remove from the whitelist (no kick) | new |
| `/allow_list` | — | Lobby (Access) | `AccessService.DescribeAllowed` | List whitelisted IDs | new |
| `/access_mode [open\|private]` | — | Lobby (Access) | `AccessService.Describe` / `SetPrivate`, `KickDenied`, `PauseGuard.SetAllowed` | Show access, or switch open / private (private kicks players without access and turns pausing on; open turns it off) | new |
| `/session_info` | — | Session | — | RiftRoulette session id, round id, map, log folder, file-logging state | new |
| `/selftest_run [all]` | — | SelfTest | `SelfTestService.Run` | Check every game dependency after a patch (PASS / WARN / FAIL) | new |
| `/selftest_live <slot>` | — | SelfTest | `SelfTestService.Live` | Teleport, restraint, banner and loadout check on one player | new |
| `/draft_status` | — | Draft | `DescribeDraft` | Both pools, every pick with player name and slot | new |
| `/draft_assign <slot> <hero>` | — | Draft | `SelectHero` | Pick a hero for a player (same rules as `/pick`) | new |
| `/draft_release <slot>` | — | Draft | `UnselectHero` | Unpick for a player (same effects as `/unpick`) | new |
| `/draft_reset` | `/reset` | Draft | `ResetDraft` | Clear the whole draft (archive effects) | archive |
| `/draft_boards` | — | Draft | `RedrawBoards` | Redraw the draft boards | new |
| `/draft_note [text]` | — | Draft | `WelcomeNoteStore.Set` + `RedrawBoards` | Set or clear the saved note under the welcome board | new |
| `/rift_start` | `/koth` | Rift | `RunRift` | Run the known-good rift sequence | archive |
| `/rift_status` | — | Rift | `DescribeRift` | Next side, phase (idle / waiting for spawn / live / ending), last outcome | new |
| `/rift_next <green\|yellow\|center>` | — | Rift | `SetNextSide`, `WatchSpot.MoveAllUp` | Force the next side; players and boards move above it | new |
| `/rift_mid <on\|off>` | — | Rift | `SetMiddleEnabled` | Include or skip the center rift in the rotation (default on) | new |
| `/rift_cancel` | — | Rift | `CancelRift` | End our round early: stop the wait/watch sequences, park the scheduler, KOTH off, return alive players to the watch spot, remove rift troopers. The rift objective already on the map stays (no known safe way to remove it) | new |
| `/rift_cleanup` | — | Rift | `CleanupRiftTroopers` | Remove every rift trooper on the map | new |
| `/spots_list [green\|yellow]` | — | Round | `SpotCheck.Describe` | Every slot's watch, Sapphire and Amber spot | new |
| `/spots_walk <watch\|sapphire\|amber> [green\|yellow]` | — | Round | `SpotCheck.Walk` | Teleport the admin through each slot spot and log where the pawn lands | new |
| `/match_start` | — | GameLoop | `MatchService.Start` | Start the continuous match loop (intermission, round, score banner, repeat) | new |
| `/match_end` | — | GameLoop | `MatchService.End` | Stop the loop, cancel a running round, final score banner, draft reset | new |
| `/match_auto <on\|off>` | — | GameLoop | `AutoStartService.SetEnabled` / `Check` | Turn match auto-start (2+ players) / auto-end (under 2) on or off | new |
| `/match_status` | — | GameLoop | `MatchService.DescribeMatch` | Match phase, round, score, ties, auto-start, intermission, rift phase (1v1: king and streak leaderboard) | new |
| `/match_intermission <seconds>` | — | GameLoop | `MatchService.SetIntermission` | Seconds between rounds (5-120, default 5) | new |
| `/match_mode <random\|draft\|duel\|1v1>` | — | GameLoop | `MatchService.SetHeroMode` | Hero mode between matches (default random; `1v1` = duel); lobby reset | new |
| `/match_format <continuous>` | — | GameLoop | `MatchService.SetFormat` | Match format between matches (only continuous for now) | new |
| `/match_config` | — | GameLoop | `MatchService.DescribeConfig` | Current mode, format, allowed values, intermission | new |
| `/random_status` | — | RandomMode | `RandomModeService.Describe` | Teams, heroes, builds, pending swaps per player | new |
| `/bet_status` | — | Betting | `BettingService.Describe` | Every player's souls and open bet; redraws the betting board | new |
| `/random_reroll` | — | RandomMode | `RandomModeService.PrepareRound` | New random heroes and builds now (Random mode intermission only) | new |
| `/duel_copy <slot>` | — | Duel | `DuelService.Copy` | Copy a player's exact hero and build for the 1v1 fighters and start (needs 2 queued) | new |
| `/duel_clear` | — | Duel | `DuelService.ClearSnapshot` | Drop the 1v1 build, back to free setup | new |
| `/duel_status` | — | Duel | `DuelService.Describe` | 1v1 build, lock, queue, players | new |
| `/duel_queue` | — | Duel | `DuelService.DescribeQueue` | 1v1 queue order, fighters, king and streak | new |
| `/duel_queue_add <slot>` | — | Duel | `DuelService.JoinQueue` | Put a player in the 1v1 queue | new |
| `/duel_queue_remove <slot>` | — | Duel | `DuelService.LeaveQueue(force)` | Take a player out of the 1v1 queue (not mid-fight) | new |
| `/stats_board` | — | Stats | `StatsService.RefreshBoards` / `DescribeAll` | Redraw the stats boards and list every player's K / D / A | new |
| `/stats_reset` | — | Stats | `StatsService.Reset` | Zero the match stats | new |
| `/balance_status` | — | Balance | `BalanceService.Describe` | Auto-balance on/off, verdict, counters since the last swap | new |
| `/balance_auto <on\|off>` | — | Balance | `BalanceService.SetEnabled` | Turn automatic balancing on or off | new |
| `/balance_now` | — | Balance | `RandomModeService.PrepareRound(forceBalance)` | Force a swap for the leading team, then reroll (Random mode intermission only) | new |

### 4.3 Admin commands — modules (Debug mode, `AdminAuth`)

Game-agnostic: no Rift Roulette names in arguments (teams are team numbers).

| Command | Archive name | Owner | Op | What it does | Status |
|---|---|---|---|---|---|
| `/wt_list` | — | WorldText | `List` | Boards created through WorldText (id, position, text preview) | new |
| `/wt_create <id> <text>` | — | WorldText | `Create` | Create a board at the caller's position and facing | new |
| `/wt_update <id> <text>` | — | WorldText | `Update` | Change a board's text | new |
| `/wt_remove <id>` | — | WorldText | `Remove` | Remove one board | new |
| `/wt_clear` | — | WorldText | `ClearAll` | Remove every `point_worldtext` (the draft redraw's first step) | new |
| `/mv_list` | — | Movement | `List` | Named locations with position and angle | new |
| `/mv_where` | — | Movement | `Where` | Caller's position and view angle (for recording new locations) | new |
| `/mv_tp <location> [slot]` | `/test` (= `/mv_tp draft`) | Movement | `TeleportTo` | Teleport caller or a slot to a location | archive |
| `/mv_tp_team <team> <location>` | — | Movement | `TeleportPlayers` | Teleport everyone on a team number | new |
| `/mv_tp_all <location>` | — | Movement | `TeleportPlayers` | Teleport every player | new |
| `/mv_angle <pitch> <yaw> <roll> [slot]` | — | Movement | `SetViewAngle` | Set a camera angle | new |
| `/mv_save <name>` | — | Movement | `Register` | Save the caller's position and angle as a location (until reload) | new |
| `/mv_remove <name>` | — | Movement | `Unregister` | Remove a location saved with `/mv_save` | new |
| `/hud_announce <title> [\| description]` | — | Hud | `AnnounceAll` | On-screen banner to every player | new |
| `/hud_say <message>` | — | Hud | `AnnounceAll` | Admin talks to the server: message as the big banner title, `Server admin` below (console `dw_hud_say`, works while spectating) | new |
| `/loadout_give <slot> <hero> [build]` | — | Loadout | `LoadoutService.Swap` | Swap a player to a hero and apply stored build 1-3 (0 = random) | new |
| `/loadout_show <slot>` | — | Loadout | `LoadoutService.Capture` / `LoadoutSnapshot.HeldLines` | Show a player's held items with soul costs, level and ability ranks (also logged) | new |
| `/loadout_copy <from> <to>` | — | Loadout | `LoadoutService.Capture` / `SwapSnapshot` | Copy one player's exact hero, items, abilities and level onto another | new |
| `/loadout_list <hero>` | — | Loadout | `HeroBuildCatalog.BuildsFor` / `Plan` | A hero's stored builds at the current cap: the items each ends with (up to 12), planned value, sold / skipped counts, optional groups | new |
| `/loadout_info` | — | Loadout | `HeroBuildCatalog.Default` | Build data date, source, hero count, baseline value, cap, banned items | new |
| `/loadout_cap [souls\|default]` | — | Loadout | `LoadoutService.SetMaxValue` | Show or set the loadout soul cap (1,000-200,000; resets to 20,000 on every load) | new |
| `/restrain <slot>` | — | Restraint | `RestraintService.Restrain` | Silence, disarm and block melee until released | new |
| `/restrain_release <slot>` | — | Restraint | `RestraintService.Release` | Lift the restraint | new |
| `/restrain_list` | — | Restraint | `RestraintService.Describe` | Restrained players and their active modifiers | new |
| `/status_add <slot> <modifier> [seconds]` | — | Restraint | `RestraintService.AddModifier` | Test any game modifier by name | new |
| `/status_remove <slot> <modifier>` | — | Restraint | `pawn.RemoveModifier` | Remove a game modifier by name | new |

### 4.4 Admin commands — tool plugins

See §2 (DevTools: `/ent_find`, `/ent_info`, `/ent_remove`,
`/ent_snapshot`, `/ent_diff`, `/dev_herowatch`, `/dev_logpath`) and §3
(CleanSlate `/cleanup_run`).

---

## 5. Console-to-logger map

Where each archive `Console.WriteLine` now logs. `P` = the line carries a
`PlayerRef`.

| Source | Line | Log | Level | P |
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
