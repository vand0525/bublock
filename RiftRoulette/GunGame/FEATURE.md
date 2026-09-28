# GunGame — Feature

## Purpose

Gun Game for Rift Roulette (Stage 13k): a match format on top of Random
mode (`/match_mode random`, `/match_format gungame`). Every kill moves the
killer one step up a kill ladder and swaps them, right away, to a new
random hero with one of that hero's real top builds, in the same budgeted
build style as the intermission draw. The first player to the target
(default 10) wins the match; the next one auto-starts 10 s later.

Everything else is Rift Roulette as it is: rounds and forced rifts, the
intermission draw, even teams and the bench, restraint up top, stats
boards (whose kill column is the ladder), betting. Players who die wait up
top for the next round, so this first version is "gun game in rounds";
a continuous arena with respawns in place is the next slice
(`reference/master-plan.md`, Stage 13k).

## Files

| File | Role |
|---|---|
| `GunGameLadder.cs` | Kills per player, target, winner, places (pure, tested) |
| `GunGameService.cs` | Match hooks, kill handling (ladder step + reroll + banner), win, target, status |
| `GunGamePlugin.cs` | `/ladder`, `/gungame_status`, `/gungame_target`, `/gungame_reroll` |

## Public operations

See `GunGameService.md`. Commands in `reference/user-commands.md` and
`reference/admin-commands.md`.

## State

`GunGameService.Ladder` (kills, winner, target setting), the names of
players with kills, and the match's `ITimer`. The target resets to 10 on
every DLL load; the ladder resets at every match start and end.

## Composition

```text
player_death ─> StatsService.RecordDeath (credits human enemy kills; skips hero lock kills)
                 ├> BalanceService.RecordKill, BettingService.OnKill   (as before)
                 └> GunGameService.OnKill (only with the gungame format in Random mode)
                      ├> GunGameLadder.Record ─ win ─> next tick: MatchService.End ─ 10 s ─> AutoStartService.Check
                      └> RandomModeService.Reroll (new hero, random build, hero lock safe)
                           └> LoadoutService.Swap (SelectHero in place, build 1 s later)
                                └> banner "Kill 3/10: <hero>" / "<build> - N souls"
MatchService.Start / End ─> GunGameService.BeginMatch / EndMatch
```

## Lifecycle vs commands

- Lifecycle: `MatchService` and `StatsService` call the service in the
  match's mode (Clean under auto-start).
- `/gungame_reroll <slot>` runs the same reroll in Debug mode without a
  ladder step, so the swap can be tested alone; `/gungame_target` sets
  the target between matches; `/ladder` is the players' view.

## Dependencies

`GameLoop/MatchConfig` (`IsGunGame`), `GameLoop/MatchService` (state,
`End`), `GameLoop/AutoStartService` (restart), `RandomMode/RandomModeService`
(`Reroll`, `BuildDescription`), `Modules/Loadout` (`HeroBuildCatalog`
names), `Modules/Hud` (banners), `Shared` (logging, auth, chat).

## Game dependencies

Nothing new: `player_death` (via Stats), `SelectHero` and the loadout calls
(via `LoadoutService`) are already in `SelfTest/GameDependencies` and
`patch-day.md` §5.

## Logs

`gungame-YYYYMMDD.log`: start, every counted kill, win, final ladder, end.
The win also goes to the master log.
