# Queue module

## Purpose

A reusable join queue of players (Stage 13j). Game agnostic: it only knows
Steam IDs and their order. First used by Rift Roulette's 1v1 mode (winner
stays on, the next in line comes in); meant for any later "wait your turn"
feature.

## Files

| File | Role |
|---|---|
| `PlayerQueue.cs` | The queue: ordered unique Steam IDs (pure, tested). |
| `Queue.projitems` | Compiles `PlayerQueue` into a consumer. |

## Public operations

`Join`, `Leave`, `Contains`, `PositionOf`, `Count`, `Items`, `Front(n)`,
`MoveToBack`, `RemoveWhere`, `Clear` (see `PlayerQueue.md`).

## State

None static. Each `PlayerQueue` instance holds its own list; the consumer
owns the instance and decides when to prune players who left.

## Commands and lifecycle

The module registers no commands and has no plugin class, so it never
clashes with another DLL. Consumers wrap it in their own commands and call
it from their own lifecycle (Rift Roulette: `DuelService`, `DuelPlugin`).

## Tests

`Tests/Modules.Tests/PlayerQueueTests.cs`.
