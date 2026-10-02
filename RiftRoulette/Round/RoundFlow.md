# RoundFlow

The composed Rift Roulette round: Rift ops (spawn, park, watch, cleanup) from
`RiftService`, plus the Movement steps that move players. The
lifecycle entry runs Clean; admin commands call the same methods in Debug.
Static; no state of its own.

## Logs

`Round` feature log (`round-YYYYMMDD.log`), Debug lines only (team move
counts, dead players skipped, players returned), so they appear only in
Debug mode. Per-player teleports are in `movement-*.log`; the rift sequence
is in `rift-*.log`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `RunRound(timer, mode)` | `RiftService.RunRift(timer, Steps(mode, timer), mode)` | reply line |
| `CancelRound(timer, mode)` | `RiftService.CancelRift(Steps(mode), timer, mode)` (the timer runs the late trooper sweeps) | reply line |
| `MoveTeamsToRift(side, mode)` | Participants (`Participants.Humans()`: no bots, no seated admin) with a round hero (`RoundHeroes.Has`; bench players have none and stay up top) on team Sapphire (`TeamNum` 3) → Sapphire start, then those on Amber (2) → Amber start (`RoundLocations.StartsFor(side)`). Each player goes to their own slot spot around the team's start, `SlotSpots.Fight(anchor, player.Slot)` (`MovementService.TeleportTo` per player), so nobody lands on a teammate. Every moved player is first released from restraint (`RestraintService.Release`), and after the teleports every one of them with a live hero is healed to full (`pawn.Heal(pawn.GetMaxHealth())`, Debug `Healed to full Health= MaxHealth=`; the move line carries `Healed=`). Grouping by `TeamNum` works because Random and Mirror mode set each fighter's team before writing their round hero | — |
| `SendPlayersUp(mode)` | Alive participants with a pawn → `WatchSpot.SendUp(player, mode, RiftService.NextSide)` (restrain + teleport above the next rift; no banner); dead ones are restrained, get a Debug line with `PlayerRef`, and are left for Lobby's `player_spawn`. Then `WatchSpot.RefreshBoards(NextSide)`. `NextSide` is explicit because the rift still counts as running here | players moved |
| `Steps(mode, probeTimer = null)` | `RiftRoundSteps` wrapping the two ops above with the same mode, plus `RoundEnded` → `GameLoop/MatchService.OnRoundEnded(result, mode)` (does nothing when no match is running). With a `probeTimer` (from `RunRound`), each move also schedules a `GameLoop/MatchProbe` snapshot 1 s later (`moved-in` / `sent-up`); `CancelRound` passes none | steps |

## Callers

- `RiftPlugin` `/rift_start`, `/rift_cancel` (Debug).
- `GameLoop/MatchService`: `RunRound` after each intermission,
  `CancelRound` on `/match_end`, in the match's mode.

## Dangerous constraints

- Never teleport a dead pawn; dead players come back through
  `player_spawn`.
- `RiftService` decides **when** the steps run (the known-good order); do
  not call `MoveTeamsToRift` / `SendPlayersUp` from elsewhere during
  a rift.
- Game-thread only.
