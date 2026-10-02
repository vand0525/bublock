# Round — Feature

## Purpose

The round, composed from Movement and Rift ops plus the round heroes:
start a rift, move each team's fighters (players in `RoundHeroes`) to its
side's start, and send players back up when the rift ends or is cancelled. One code path; the mode (Clean for
lifecycle, Debug for admin) changes only logging. The round reports its
result to the match loop. Players wait at a watch spot above the rift
(`WatchSpot`), not the fixed `draft` spot, restrained (`Modules/Restraint`)
until they are moved into the rift, where each fighter is healed to full
health.

## Files

| File | Role |
|---|---|
| `RoundHeroes.cs` | This round's fighters: Steam ID → hero, written by Random / Mirror mode (pure data, tested) |
| `RoundLocations.cs` | Side → (Sapphire, Amber) start locations, side → watch spot (pure) |
| `WatchSpotRule.cs` | Which side the watch spot is above (pure) |
| `WatchSpot.cs` | Send players up (teleport + restrain), move everyone up, move the boards |
| `WatchLayout.cs` | Board offsets / yaw turned half a turn on yellow, look-at angles (pure) |
| `WatchGuardRule.cs` | "Below the watch spot" line: spot z - 300 (pure) |
| `WatchGuard.cs` | Every 16 frames sends restrained players who dropped below the line back up (no banner); logs restrained players' console commands |
| `RoundFlow.cs` | Composer: `RunRound`, `CancelRound`, `MoveTeamsToRift`, `SendPlayersUp`, `Steps` |
| `SlotSpots.cs` + `Data/spots.json` | Per-slot spots: anchor + slot offset (forward, right, up) for the watch spot and the rift starts (embedded table) |
| `SpotCheck.cs` | List the slot spots; walk an admin through one group and log where the pawn lands |
| `SpotsPlugin.cs` | `/spots_list`, `/spots_walk` |

## Public operations

See `RoundFlow.md`. `RiftPlugin` hosts `/rift_start` and `/rift_cancel`,
which call `RoundFlow` in Debug mode. `SpotsPlugin` hosts `/spots_list` and
`/spots_walk` (admin, Debug).

## Slot spots

Every teleport up top (`WatchSpot.SendUp`) and into a rift
(`RoundFlow.MoveTeamsToRift`) goes to the player's own spot:
`SlotSpots.Watch(anchor, player.Slot)` / `SlotSpots.Fight(teamAnchor, player.Slot)`.
The anchors are the `RiftRouletteLocations` watch spots and team starts;
moving an anchor moves its whole group, and editing `spots.json` moves one
slot. `scripts/check-spots.py` checks every spot against the map mesh
(floor, room for a hero, no wall between anchor and spot); `/spots_walk`
confirms in game.

## State

`RoundHeroes` holds this round's fighters (Steam ID → hero);
`WatchSpot` remembers which side the boards were last drawn at;
`WatchGuard` keeps per-player grace times, a command-log repeat filter and a
rescue count. Round state
(phase, side, snapshot, timers) is owned by `Rift/RiftService`; restraint
by `Modules/Restraint`.

## Composition

```text
GameLoop/MatchService ─────────────┐
                                   ├─> RoundFlow.RunRound / CancelRound
/rift_start, /rift_cancel (Debug) ─┘
      └─> RiftService.RunRift / CancelRift (order, gamerules, watch, cleanup)
            └─ steps ─> RoundFlow.MoveTeamsToRift  (RoundHeroes entry + TeamNum; release restraint; MovementService; heal to full)
                     ├> RoundFlow.SendPlayersUp (WatchSpot.SendUp above NextSide, boards follow)
                     └> MatchService.OnRoundEnded (score + banner; no-op outside a match)
```

## Dependencies

- `Rift/RiftService`, `Rift/RiftSide`, `Rift/RiftRoundResult`.
- `GameLoop/MatchService` (round-ended step only).
- `Boards/BoardService` (board redraw), `Lobby/RiftRouletteTeams`.
- `Modules/Movement`, `Modules/Restraint`, `Locations/RiftRouletteLocations`.
- `Shared` (logging, `ExecutionMode`).

## Logs

`round-YYYYMMDD.log`: Debug lines, plus an Info line when `/rift_next`
moves everyone to the watch spot. `watch-YYYYMMDD.log`: rescues and
restrained players' console commands (`WatchGuard`, Information).
`RoundFlow.Steps(mode, timer)` also schedules `GameLoop/MatchProbe`
snapshots 1 s after each move (`probe-YYYYMMDD.log`).

## Hooks

`WatchGuard` runs from `GameLoopPlugin.OnGameFrame` / `OnClientConCommand`
(`SpotsPlugin` has commands only, no hooks). `spots-YYYYMMDD.log` holds the
`/spots_walk` results.
