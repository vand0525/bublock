# ArenaService

Sends a player to their own arena spot through `Modules/Movement`.
Game-agnostic: the game type passes its `ArenaSpots`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `SendToArena(player, arena, mode)` | `arena.For(team, slot)`, then `MovementService.TeleportTo` (position, zero velocity, camera turned to the anchor's angle). A team that is not Amber / Sapphire, or no pawn: nothing (Debug line) | sent |
| `ReturnStrays(players, arena, mode)` | Containment: each living player whose pawn is outside `arena.Bounds` is logged (`Left the arena, sent back`) and sent to their spot. No bounds: does nothing | players returned |
| `SendToArenaNextTick(player, arena, timer, mode)` | For spawn hooks: finds the player again by slot and Steam ID on the next tick, then `SendToArena` | — |

## Logs

`arena-YYYYMMDD.log` in the consuming DLL's log folder (Debug lines).

## Deadworks constraints

- Teleport outside game events (next tick), as Rift Roulette does for its
  watch spot.
