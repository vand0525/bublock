# Rift Roulette — Feature

## Purpose

Custom Deadlock game mode plugin: draft/staging, hero select, real Rift (KOTH)
rounds, return to draft. See `reference/chat-handoff.md` and
`reference/master-plan.md`.

## Current state

- All archive behavior lives in feature plugin classes; the Legacy monolith
  was deleted in Stage 11.
- Compiles in `Bublock/Shared/` via `Shared.projitems`.
- Compiles in `Modules/WorldText` (`WorldText.projitems` +
  `WorldTextCommands.projitems`): `WorldTextPlugin` hosts the `/wt_*` admin
  commands, and Draft draws its boards through `WorldTextService` (Stage 6).
- Compiles in `Modules/Movement` (`Movement.projitems` +
  `MovementCommands.projitems`): `MovementPlugin` hosts the `/mv_*` admin
  commands; Lobby, Draft, and Round teleport through `MovementService` using
  the typed positions in `Locations/RiftRouletteLocations.cs` (Stage 7).
- `Lobby/LobbyPlugin` owns startup convars, connect / disconnect / spawn /
  death hooks, kick, team moves, `/status` (Stage 8), and the player
  command list `/commands` (Stage 12).
- `Draft/DraftPlugin` owns hero pools, picks, hero enforcement, starting
  progression, draft reset, and the draft boards (Stage 9). Picks live in
  `Draft/DraftState`, read by Lobby and Round.
- `Rift/RiftPlugin` and `RiftService` own the rift itself: spawn, park,
  watch, end round, cleanup, green / yellow alternation (Stage 10).
- `Round/RoundFlow` composes a round from Rift, Draft, and Movement ops
  (team moves, return to draft). It is the Clean lifecycle entry; the admin
  commands call it in Debug (Stage 11).
- `GameLoop/GameLoopPlugin` and `MatchService` run the continuous playtest
  match: `/match_start` once, then intermission → round → score banner,
  repeated until `/match_end` (Stage 13a).
- Compiles in `Modules/Hud` (`Hud.projitems` + `HudCommands.projitems`):
  on-screen banners through `HudService`; `HudPlugin` hosts
  `/hud_announce` (Stage 13a).
- `GameLoop/MatchConfig` picks the hero mode: `random` (default) or
  `draft` (Stage 13b). In Random mode `RandomMode/RandomPlugin` and
  `RandomModeService` balance teams once and give everyone a new random
  hero and top build every intermission.
- Stage 13c: `Stats/` counts kills, deaths and assists per match and shows
  them on the Sapphire and Amber boards (Random mode); `Balance/` swaps
  players when one team stomps or streaks; new connections join the smaller
  team (`Lobby/TeamBalance`) and get a hero right away during a Random
  intermission; changing hero from the menu in Random mode kills the player,
  who respawns with their assigned hero and build.
- Stage 13d: `GameLoop/AutoStartService` starts the match when 2 human
  players are connected and ends it when fewer than 2 remain
  (`/match_auto <on|off>`), so no admin needs to be online.
- Stage 13e: Cultist Sacrifice joins Monster Rounds and Golden Goose Egg on
  the banned-items list (never in a Random mode build).
- Stage 13f: the server takes 13 connections (browser shows 12); the 13th
  is an admin-only seat on the spectator side (`Lobby/AdminSeat`,
  `dw_seat_spec` console only and any time, `/seat_play`,
  `/seat_status`). Admins are seated on every connect. A seated admin is
  left out of teams, heroes, rounds, stats and auto-start
  (`Lobby/Participants`).
- Stage 13g: 1v1 mode (`/match_mode 1v1`, `Duel/`): free hero switching and
  100,000 souls while a build is prepared, then `/duel_copy <slot>` copies
  that player's exact hero, items, abilities and level onto both players
  (`Modules/Loadout` snapshot) and starts the match; both stay locked to it
  (`Lobby/HeroLock`) and are reset to it every intermission.
- Stage 13h: players up top (lobby, between rounds, after dying) are
  silenced and blocked from items, shooting and melee until they are moved
  into the rift (`Modules/Restraint`, game modifier
  `modifier_citadel_silenced` plus modifier states set every frame; no
  disarm, so they can reload).
- Stage 13i: the waiting spot moves above the rift being fought, or the
  next one between rounds (`Round/WatchSpot`); it only changes when
  players are sent back up after a round, and the boards move with it.
- Stage 13j: 1v1 mode is winner-stays-on with a join queue (`/queue`,
  `/unqueue`; reusable `Modules/Queue`): the first two queued fight, the
  loser goes to the back, the winner builds a streak.
- Stage 13k (redock fork): Gun Game format (`/match_format gungame`, Random
  mode, `GunGame/`): every credited kill moves the killer up a kill ladder
  and swaps them to a new random hero and top build
  (`RandomModeService.Reroll`); the first to the target (default 10) wins,
  and the next match auto-starts.
- Compiles in `Modules/Loadout` (`Loadout.projitems` +
  `LoadoutCommands.projitems`): the embedded top-3 builds per hero
  (`Data/hero-builds.json`, from `scripts/fetch-builds.py`) and
  `LoadoutService` (reset, level, abilities, first 9 items); `LoadoutPlugin`
  hosts `/loadout_*` (Stage 13b).
- `Session/SessionPlugin.cs` writes session lifecycle lines to
  `bublock/logs/RiftRoulette/master-*.log`; every feature writes its own
  feature log, and no Rift Roulette code prints diagnostics to the console.
- Builds under `Bublock/` against workspace `lib/`; ships as
  `RiftRoulette.dll` (renamed from `RiftRumble.dll` on 2026-09-27; deploy deletes the old DLL).
- First server push was Stage 12 (2026-09-27); every push goes through
  `scripts/deploy.sh --confirm` with the user's approval.

## Breakdown

`RiftRoulette.dll` hosts many small plugin classes in one load context, so
they call each other with typed C# (no command/convar messaging):

| Folder | Plugin class | Owns | Stage |
|--------|--------------|------|-------|
| `Modules/WorldText/` (compiled in) | `WorldTextPlugin` | boards / in-game text | 6 (done) |
| `Modules/Movement/` (compiled in) | `MovementPlugin` | named locations, teleports | 7 (done) |
| `Locations/` | — (data) | Rift Roulette positions registered with Movement | 7 (done) |
| `Lobby/` | `LobbyPlugin` | connect/disconnect/spawn/death, teams, kick, status, startup convars, admin seat, participants, hero lock | 8 (done), 13f, 13g |
| `Draft/` | `DraftPlugin` | hero pools, selections, enforcement, starting progression, draft boards | 9 (done; `DraftState` since 8) |
| `Rift/` | `RiftPlugin` | spawn / park / watch / cleanup / alternation | 10 (done) |
| `Round/` | — (composer) | the parity round: Rift ops + team moves + return to the watch spot | 11 (done), 13i |
| `Modules/Hud/` (compiled in) | `HudPlugin` | on-screen banners | 13a (done) |
| `GameLoop/` | `GameLoopPlugin` | continuous match loop, score, match end, match config, auto-start | 13a (done), 13b, 13d |
| `Modules/Loadout/` (compiled in) | `LoadoutPlugin` | stored top builds, apply build to a pawn | 13b |
| `RandomMode/` | `RandomPlugin` | Random mode: teams, per-round hero + build, pending swaps, joiners, hero guard | 13b, 13c |
| `Duel/` | `DuelPlugin` | 1v1 mode: copied build, hero lock, setup souls, queue, winner stays on | 13g, 13j |
| `Modules/Restraint/` (compiled in) | `RestraintPlugin` | silence / no items, shooting or melee until released | 13h |
| `Modules/Queue/` (compiled in) | — (data) | reusable player queue | 13j |
| `Stats/` | `StatsPlugin` | match kills / deaths / assists, stats boards | 13c |
| `Balance/` | `BalancePlugin` | auto-balance trigger and swaps | 13c |
| `Betting/` | `BettingPlugin` | round betting with kill chips, betting board (Random mode) | 13 |
| `Session/` | `SessionPlugin` | session lifecycle lines in the master log | 4 (done) |
| `SelfTest/` | `SelfTestPlugin` | patch-day self-test of every game dependency, hook counters | patch-day readiness |

`Shared/` (auth, cheats, convars, execution mode, logging) is compiled in as well.

## Public surface (today)

- Draft (`DraftPlugin`): player `/pick`, `/unpick`, `/picks`, `/heroes`;
  admin `draft_status`, `draft_assign`, `draft_release`, `draft_reset`,
  `draft_boards`
- Rift (`RiftPlugin`): admin `rift_start`, `rift_status`, `rift_next`,
  `rift_cancel`, `rift_cleanup`
- Lobby (`LobbyPlugin`): player `/status`, `/commands`; admin
  `player_list`, `player_info`, `player_kick`, `player_team`, `lobby_setup`,
  `seat_spec` (console only), `seat_play`, `seat_status`
- GameLoop (`GameLoopPlugin`): player `/score`; admin `match_start`,
  `match_end`, `match_auto`, `match_status`, `match_intermission`, `match_mode`,
  `match_format`, `match_config`
- Random (`RandomPlugin`): admin `random_status`, `random_reroll`
- Duel (`DuelPlugin`): player `/queue`, `/unqueue`; admin `duel_copy`,
  `duel_clear`, `duel_status`, `duel_queue`, `duel_queue_add`,
  `duel_queue_remove`
- Restraint admin commands (`RestraintPlugin`): `restrain`,
  `restrain_release`, `restrain_list`, `status_add`, `status_remove`
- Stats (`StatsPlugin`): player `/stats`; admin `stats_board`, `stats_reset`
- Balance (`BalancePlugin`): admin `balance_status`, `balance_auto`,
  `balance_now`
- Betting (`BettingPlugin`): players type `sapphire` / `amber` in chat or
  `/bet <team>`, `/chips`; admin `bet_status`
- Loadout admin commands (`LoadoutPlugin`): `loadout_give`,
  `loadout_copy`, `loadout_list`, `loadout_info`
- Gun Game (`GunGamePlugin`): player `/ladder`; admin `gungame_status`,
  `gungame_target`, `gungame_reroll`
- Hud admin commands (`HudPlugin`): `hud_announce`, `hud_say`
- No archive command names remain (hidden aliases removed in Stage 12).
- Session (`SessionPlugin`): admin `session_info`
- SelfTest (`SelfTestPlugin`): admin `selftest_run`, `selftest_live`
- WorldText admin commands (`WorldTextPlugin`): `wt_list`, `wt_create`,
  `wt_update`, `wt_remove`, `wt_clear`
- Movement admin commands (`MovementPlugin`): `mv_list`, `mv_where`, `mv_tp`,
  `mv_tp_team`, `mv_tp_all`, `mv_angle`, `mv_save`, `mv_remove`
- Hooks: startup convars, player spawn/death, connect/disconnect (Lobby);
  boards next tick after startup and `player_hero_changed` enforcement
  (Draft, with the 1v1 and Random mode hero locks); `player_respawned` /
  `player_spawn` pending loadouts (Random, 1v1) and 1v1 setup souls (Duel);
  `player_death` stats (Stats); `OnClientConnect` admin-seat gate (Lobby);
  `OnGameFrame` restraint upkeep (Restraint)

## State ownership

Draft owns picks (`Draft/DraftState`), Rift owns the next side, phase,
trooper snapshot, and rift timers (`Rift/RiftService`), GameLoop owns the
match score and countdown (`GameLoop/MatchService`) and the config
(`GameLoop/MatchConfig`), Random owns teams / assignments / pending swaps
(`RandomMode/RandomModeService`), Duel owns the copied build, lock and
teams, queue and streak (`Duel/DuelService`), Restraint owns the
restrained players (`Modules/Restraint`), Round's `WatchSpot` remembers the
board side, Lobby owns the admin seat (`Lobby/AdminSeat`),
GunGame owns the kill ladder and target (`GunGame/GunGameService`),
Stats owns the match ledger
(`Stats/StatsService`), Balance owns the swap counters
(`Balance/BalanceService`), Loadout owns the read-only build catalog,
WorldText owns board entities, and Movement owns the location registry.
Round owns nothing else.

## Composition

```text
GameLoop/MatchService (Stage 13a) ─┐
                                   ├─> RoundFlow.RunRound / CancelRound
/rift_start, /rift_cancel (Debug) ─┘
      └─> RiftService (order, gamerules, watch, cleanup)
            └─ steps ─> RoundFlow.MoveTeamsToRift (pick or 1v1 fighter + TeamNum; release restraint)
                     ├> RoundFlow.ReturnPlayersToDraft (WatchSpot.SendUp above NextSide: restrain + teleport; boards follow)
                     └> MatchService.OnRoundEnded (score, HudService banner; 1v1: DuelService.RecordResult)
                          └─> next intermission: RandomModeService.PrepareRound
                                (BenchRule: odd count sits one out → BalanceService.TryBalance →
                                 HeroDraw → LoadoutService.Swap per fighter)
player_death ─> StatsService.RecordDeath ─> BalanceService.RecordKill, stats boards
spawn ─> WatchSpot.SendUp (restrained, above the rift being fought)
connect ─> LobbyService.AdmitPlayer (smaller team, WatchSpot.SendUp) ─> RandomModeService.AddJoiner
        └─> AutoStartService.Check (2+ humans: MatchService.Start)
disconnect ─> LobbyService.RemovePlayer ─> RandomModeService.OnLeave (bench subs in) ─> AutoStartService.Check (under 2: MatchService.End)

AdminCommand  → same ops (Debug ON)
PlayerCommand → same ops (Clean by default)
```

The match loop is the automatic trigger for rounds; `/rift_start` still
runs one round by hand. Do not compose via `Server.ExecuteCommand("dw_…")`.
