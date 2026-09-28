# Rift Roulette — Feature

## Purpose

Custom Deadlock game mode plugin: draft/staging, hero select, real Rift (KOTH)
rounds, return to draft. See `reference/chat-handoff.md` and
`reference/master-plan.md`.

## Current state

- All behavior lives in feature plugin classes.
- Includes `Bublock/Shared/`.
- Includes `Modules/WorldText`: `WorldTextPlugin` hosts the `/wt_*` admin
  commands, and Draft draws its boards through `WorldTextService`.
- Includes `Modules/Movement`: `MovementPlugin` hosts the `/mv_*` admin
  commands; Lobby, Draft, and Round teleport through `MovementService` using
  the typed positions in `Locations/RiftRouletteLocations.cs`.
- `Lobby/LobbyPlugin` owns startup convars, connect / disconnect / spawn /
  death hooks, kick, team moves, `/status`, and the player command list
  `/commands`.
- `Draft/DraftPlugin` owns hero pools, picks, hero enforcement, starting
  progression, draft reset, and the draft boards. Picks live in
  `Draft/DraftState`, read by Lobby and Round.
- `Rift/RiftPlugin` and `RiftService` own the rift itself: spawn, park,
  watch, end round, cleanup, green / yellow alternation.
- `Round/RoundFlow` composes a round from Rift, Draft, and Movement ops
  (team moves, return to draft). It is the Clean lifecycle entry; the admin
  commands call it in Debug.
- `GameLoop/GameLoopPlugin` and `MatchService` run the continuous playtest
  match: `/match_start` once, then intermission → round → score banner,
  repeated until `/match_end`.
- Includes `Modules/Hud`: on-screen banners through `HudService`;
  `HudPlugin` hosts `/hud_announce`.
- `GameLoop/MatchConfig` picks the hero mode: `random` (default) or
  `draft`. In Random mode `RandomMode/RandomPlugin` and
  `RandomModeService` balance teams once and give everyone a new random
  hero and top build every intermission.
- `Stats/` counts kills, deaths and assists per match and shows them on the
  Sapphire and Amber boards (Random mode); `Balance/` swaps players when
  one team stomps or streaks; new connections join the smaller team
  (`Lobby/TeamBalance`) and get a hero right away during a Random
  intermission; changing hero from the menu in Random mode kills the player,
  who respawns with their assigned hero and build.
- `GameLoop/AutoStartService` starts the match when 2 human players are
  connected and ends it when fewer than 2 remain (`/match_auto <on|off>`),
  so no admin needs to be online.
- Cultist Sacrifice, Monster Rounds and Golden Goose Egg are on the
  banned-items list (never in a Random mode build).
- The server takes 13 connections (browser shows 12); the 13th is an
  admin-only seat on the spectator side (`Lobby/AdminSeat`,
  `dw_seat_spec` console only and any time, `/seat_play`,
  `/seat_status`). Admins are seated on every connect. A seated admin is
  left out of teams, heroes, rounds, stats and auto-start
  (`Lobby/Participants`).
- 1v1 mode (`/match_mode 1v1`, `Duel/`): free hero switching and
  100,000 souls while a build is prepared, then `/duel_copy <slot>` copies
  that player's exact hero, items, abilities and level onto both players
  (`Modules/Loadout` snapshot) and starts the match; both stay locked to it
  (`Lobby/HeroLock`) and are reset to it every intermission.
- Players up top (lobby, between rounds, after dying) are silenced and
  blocked from items, shooting and melee until they are moved into the
  rift (`Modules/Restraint`, game modifier `modifier_citadel_silenced`
  plus modifier states set every frame; no disarm, so they can reload).
- The waiting spot sits above the rift being fought, or the next one
  between rounds (`Round/WatchSpot`); it only changes when players are
  sent back up after a round, and the boards move with it.
- 1v1 mode is winner-stays-on with a join queue (`/queue`, `/unqueue`;
  reusable `Modules/Queue`): the first two queued fight, the loser goes to
  the back, the winner builds a streak.
- Includes `Modules/Loadout`: the embedded top-3 builds per hero
  (`Data/hero-builds.json`, from `scripts/fetch-builds.py`) and
  `LoadoutService` (reset, level from the cap, abilities, items shopped
  within the cap into 12 slots); `LoadoutPlugin` hosts `/loadout_*`.
- `Session/SessionPlugin.cs` writes session lifecycle lines to
  `bublock/logs/RiftRoulette/master-*.log`; every feature writes its own
  feature log, and no Rift Roulette code prints diagnostics to the console.
- Builds under `Bublock/` against workspace `lib/`; ships as
  `RiftRoulette.dll` (deploy deletes the retired `RiftRumble.dll`, which would otherwise load alongside it).
- Every server push goes through `scripts/deploy.sh --confirm` with the
  user's approval.

## Breakdown

`RiftRoulette.dll` hosts many small plugin classes in one load context, so
they call each other with typed C# (no command/convar messaging):

| Folder | Plugin class | Owns |
|--------|--------------|------|
| `Modules/WorldText/` (module) | `WorldTextPlugin` | boards / in-game text |
| `Modules/Movement/` (module) | `MovementPlugin` | named locations, teleports |
| `Locations/` | — (data) | Rift Roulette positions registered with Movement |
| `Lobby/` | `LobbyPlugin` | connect/disconnect/spawn/death, teams, kick, status, startup convars, admin seat, participants, hero lock |
| `Draft/` | `DraftPlugin` | hero pools, selections, enforcement, starting progression, draft boards |
| `Rift/` | `RiftPlugin` | spawn / park / watch / cleanup / alternation |
| `Round/` | — (composer) | the parity round: Rift ops + team moves + return to the watch spot |
| `Modules/Hud/` (module) | `HudPlugin` | on-screen banners |
| `GameLoop/` | `GameLoopPlugin` | continuous match loop, score, match end, match config, auto-start |
| `Modules/Loadout/` (module) | `LoadoutPlugin` | stored top builds, apply build to a pawn |
| `RandomMode/` | `RandomPlugin` | Random mode: teams, per-round hero + build, pending swaps, joiners, hero guard |
| `Duel/` | `DuelPlugin` | 1v1 mode: copied build, hero lock, setup souls, queue, winner stays on |
| `Modules/Restraint/` (module) | `RestraintPlugin` | silence / no items, shooting or melee until released |
| `Modules/Queue/` (module) | — (data) | reusable player queue |
| `Stats/` | `StatsPlugin` | match kills / deaths / assists, stats boards |
| `Balance/` | `BalancePlugin` | auto-balance trigger and swaps |
| `Betting/` | `BettingPlugin` | round betting with kill souls, betting board (Random mode) |
| `Session/` | `SessionPlugin` | session lifecycle lines in the master log |
| `SelfTest/` | `SelfTestPlugin` | patch-day self-test of every game dependency, hook counters |

`Shared/` (auth, cheats, convars, execution mode, logging) is included as well.

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
- Random (`RandomPlugin`): player `/reserve [hero]`; admin `random_status`, `random_reroll`
- Duel (`DuelPlugin`): player `/queue`, `/unqueue`; admin `duel_copy`,
  `duel_clear`, `duel_status`, `duel_queue`, `duel_queue_add`,
  `duel_queue_remove`
- Restraint admin commands (`RestraintPlugin`): `restrain`,
  `restrain_release`, `restrain_list`, `status_add`, `status_remove`
- Stats (`StatsPlugin`): player `/stats`; admin `stats_board`, `stats_reset`
- Balance (`BalancePlugin`): admin `balance_status`, `balance_auto`,
  `balance_now`
- Betting (`BettingPlugin`): players type `sapphire` / `amber` in chat or
  `/bet <team>`, `/souls`; admin `bet_status`
- Loadout admin commands (`LoadoutPlugin`): `loadout_give`,
  `loadout_copy`, `loadout_list`, `loadout_info`
- Hud admin commands (`HudPlugin`): `hud_announce`, `hud_say`
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
Stats owns the match ledger
(`Stats/StatsService`), Balance owns the swap counters
(`Balance/BalanceService`), Loadout owns the read-only build catalog,
WorldText owns board entities, and Movement owns the location registry.
Round owns nothing else.

## Composition

```text
GameLoop/MatchService ─────────────┐
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
