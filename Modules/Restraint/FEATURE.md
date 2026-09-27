# Restraint — Feature

## Purpose

Reusable, game-agnostic "can't fight" state: silence, item block, shooting
block and a melee block that stay on a player until released, through death
and hero swaps. Reloading still works (no disarm). Restrained players are
also ignored by NPC targeting, and `IsRestrainedPawn` lets the consumer's
damage hook make them take no damage (Rift Roulette blocks all damage to
players waiting up top).
Uses the game's real silence modifier so its status icon shows. Added in
Stage 13h.

## Public operations

| Op | Doc |
|---|---|
| `RestraintService.Restrain(player, mode)` | `RestraintService.md` |
| `RestraintService.Release(player, mode)` | `RestraintService.md` |
| `RestraintService.Forget(steamId)` | `RestraintService.md` |
| `RestraintService.Sustain()` | `RestraintService.md` |
| `RestraintService.AddModifier(pawn, name, seconds)` | `RestraintService.md` |
| `RestraintService.IsRestrainedPawn(entity)` | `RestraintService.md` |

## State

`RestraintService` holds the restrained Steam IDs and a frame counter.

## Units

| File | Role |
|---|---|
| `RestraintService.cs` | Restrain / release / per-frame upkeep |
| `RestraintPlugin.cs` | `OnGameFrame` hook, `/restrain*` and `/status_*` admin commands |
| `Restraint.projitems` | Service (no commands) |
| `RestraintCommands.projitems` | Plugin class |

## Lifecycle vs commands

- Rift Roulette restrains a player whenever it sends them up top
  (`Round/WatchSpot.SendUp`: lobby join, respawn, round end, unpick, draft
  reset) and releases the fighters in `RoundFlow.MoveTeamsToRift` just
  before the teleport. Disconnects call `Forget`; the admin seat releases.
- Admin commands call the same ops in Debug mode; `/status_add` /
  `/status_remove` test any modifier by name. See
  `RiftRoulette/reference/admin-commands.md`.

## Consumers

- `RiftRoulette.dll` imports both projitems.
