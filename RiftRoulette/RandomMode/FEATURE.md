# RandomMode — Feature

## Purpose

Random hero mode for the continuous match (Stage 13b, the default
`MatchConfig.HeroMode`). No draft and no shepherding: at match start the
teams are evened out (current teams kept where possible), and every
intermission each player is swapped to a new random hero with one of that
hero's top 3 real builds (the build's items, one random pick per optional
group, capped at 20,000 souls; the build's ability order, level 36, 0
souls). Later, heroes will come from a real match ID; only `HeroDraw`
changes then.

Stage 13c adds: players joining mid-match (a hero right away during an
intermission), team evening (`TeamBalance.Even`, when someone left or
spectated) then auto-balance before each draw (`Balance/`), and the hero swap
guard (changing hero from the menu kills you; you respawn with your
assigned hero and full build).

## Files

| File | Role |
|---|---|
| `HeroDraw.cs` | Unique random heroes, no repeat of last round (pure, tested) |
| `RandomModeService.cs` | Match / round orchestration, joiners, pending swaps, hero guard, banners, status |
| `RandomPlugin.cs` | `player_respawned` + `player_spawn` hooks, `/random_status`, `/random_reroll` |

Team placement (`TeamBalance`) lives in `Lobby/` since Stage 13c.

## Public operations

See `RandomModeService.md`. Admin commands in
`reference/admin-commands.md`.

## State

`RandomModeService` statics: teams, last heroes, current assignments, and a
`Lobby/HeroLock` (pending set, applied set, enforcement kills; one per DLL
load; cleared by `BeginMatch` / `EndMatch`). 1v1 mode owns its own lock.

## Dependencies

- `Modules/Loadout` (`HeroBuildCatalog`, `LoadoutService.Swap`).
- `Modules/Hud` (per-player build banner: hero, build, soul value),
  `Shared` (`PlayerChat` for the hero-lock line, logging, auth).
- `Draft/DraftState` (assignments are written as picks), `Lobby/RiftRouletteTeams`,
  `Lobby/TeamBalance`.
- `Balance/BalanceService` (auto-balance), `Stats/StatsService` (board refresh).
- `GameLoop/MatchConfig`, `GameLoop/MatchService.State` (reroll / joiner gate).

## Lifecycle vs commands

- `GameLoop/MatchService` calls `BeginMatch` in `Start`, `PrepareRound` at
  the start of every intermission (not on a failed-start retry),
  `AnnounceUpcoming` at the 5 s warning, and `EndMatch` in `End`. These run
  in the match's mode (Debug when an admin started the match).
- `Lobby/LobbyService.AdmitPlayer` calls `AddJoiner` for players who connect
  during a Random match.
- `Draft/DraftService.EnforceHero` calls `DuelService.GuardHero`, then
  `GuardHero`.
- `Lobby/AdminSeat.Sit` calls `Forget`.
- `/random_reroll` calls the same `PrepareRound` in Debug mode;
  `/balance_now` calls it with `forceBalance: true`.
- In Random mode, Draft's pool boards are replaced by the stats boards and
  `/pick`, `/unpick`, `/heroes`, and `/draft_assign` reply that heroes are
  random.

## Logs

`random-YYYYMMDD.log`; per-player loadout lines in `loadout-YYYYMMDD.log`.
