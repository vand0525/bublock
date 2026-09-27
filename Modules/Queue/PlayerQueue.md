# PlayerQueue

An ordered line of unique Steam IDs. Pure C# (no Deadworks calls), game
agnostic, unit tested in `Tests/Modules.Tests`. Not static: each consumer
owns its own instance (Rift Roulette's 1v1 mode owns one in `DuelService`).

## Operations

| Operation | Result |
|---|---|
| `Join(steamId)` | Adds to the back if missing; returns the 1-based position either way. |
| `Leave(steamId)` | Removes; false if not queued. |
| `Contains(steamId)` | Whether queued. |
| `PositionOf(steamId)` | 1-based position, or null. |
| `Count` / `Items` | Size and the current order (front first). |
| `Front(n)` | Copy of the first `n` IDs (fewer if the queue is shorter; none for `n <= 0`). |
| `MoveToBack(steamId)` | Moves a queued ID to the back; false if not queued. |
| `RemoveWhere(predicate)` | Removes every matching ID; returns how many. |
| `Clear()` | Empties the queue. |

## Invariants

- No duplicates: `Join` of a queued ID keeps its place.
- Order only changes through `Join` (append), `Leave` / `RemoveWhere`
  (close the gap) and `MoveToBack`.
- Not thread safe; Deadworks calls plugins on the game thread.
