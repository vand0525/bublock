# Pit — Feature

## Purpose

The first playable Crystal Crackers round, run by admin commands (no
lobby or match loop yet). Attack / defense around the mid-boss pit: one
team starts in the pit with the boss, the other in mid lane on its own
side. Kill the boss, then a crystal window; the team that takes the
crystal wins, else the boss killer. Dead fighters are out for the round
unless the crystal's rebirth brings them back.

## Files

| File | Role |
|---|---|
| `PitLocations.cs` | Anchors `pit_watch`, `pit_sapphire`, `pit_amber`, `lane_sapphire`, `lane_amber` |
| `Data/spots.json` | Per-slot offsets: `watch`, `pit`, `lane` (13 each), embedded |
| `PitSpots.cs` | Slot to spot (anchor + offset) |
| `PitRoundRule.cs` | Pure decisions: phases, pit team, winner, respawn action, timing ranges |
| `PitRoundService.cs` | The round state machine |
| `PitRoundPlugin.cs` | Hooks and `/pit_*` commands |

## Public operations

`PitRoundService.Start`, `End`, `SetTiming`, `Describe`, `OnBossKilled`,
`OnCrystalTaken`, `WatchCredit`, `OnDeath`, `OnSpawn`, `ResetForMap`,
`Forget`.

## State

`PitRoundService` (in memory): round number, score, pit team, fighters,
out set, pending swaps, timing. A hot reload starts at round 0, `Idle`.

## Composition

- `Modules/RandomHeroes` (`HeroDraw`), `Modules/Loadout` (`Swap`,
  `MaxValue`, `SoulRule`), `Modules/Restraint`, `Modules/Movement`,
  `Modules/Hud`, `Modules/MidBoss` (`Ready`, `Recheck`, `BossKilled`,
  `CrystalTaken`, `HasRebirth`, `ClearRejuv`, `RemoveBosses`,
  `RemoveCrystals`).
- Lifecycle: `player_death`, `player_spawn`, `OnModifyCurrency`, the
  MidBoss events and a 1 s credit scan drive the round once started.
- Admin commands: `/pit_start`, `/pit_end`, `/pit_status`,
  `/pit_timing` (Debug mode). No player commands yet.

## Checks

- `scripts/check-spots.py --mode crystal`: every spot clear of the map
  mesh.
- `Tests/CrystalCrackers.Tests`: `PitRoundRuleTests`, `PitSpotsTests`.
