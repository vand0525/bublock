# WorldTextService

Core operations for in-game text boards (`point_worldtext`). Static service:
no plugin-class dependency, no timers, created on first use.

## State

- Registry of boards created through this service: string id
  (case-insensitive) -> board entity + `WorldTextSpec`. Never keyed by
  EntityIndex (not globally unique).
- Entries whose entity is gone (`IsValid == false`, e.g. after a map change
  or an outside removal) are dropped the next time they are touched or
  listed.

## Operations

Every op takes `ExecutionMode mode = Clean` and logs to the `WorldText`
feature log (`worldtext-YYYYMMDD.log`, prefix `[<Dll>.WorldText]`) via
`WithMode(mode)`. Per-board lines are `Debug`, so they only appear for admin
(Debug) invocations.

| Op | Behavior | Returns |
|---|---|---|
| `Create(id, spec, mode)` | Removes any existing board with that id, then `CPointWorldText.Create(text, position, fontSize, worldUnitsPerPx, r, g, b, a, reorientMode)` followed by `Teleport(position, angle, Vector3.Zero)`, exactly like the archive `CreateBoard`. Warning if the create returns null | `bool` |
| `Update(id, text, mode)` | `SetMessage(text)` on a live board and stores the new text | `false` if no live board with that id |
| `Remove(id, mode)` | Removes one tracked board | `false` if no live board with that id |
| `ClearAll(mode)` | Removes **every** `point_worldtext` on the map (tracked or not, archive semantics) and empties the registry | count removed |
| `List()` | Live tracked boards as `BoardInfo(Id, Position, Text, Angle)`, sorted by id | list |

## Dangerous constraints

- `ClearAll` also removes text entities this service did not create (the
  archive redraw did the same). Use `Remove(id)` for targeted cleanup.
- `Remove()` on an entity is deferred to end of frame; creating a new board
  on the same tick is fine (the archive redraw does exactly that).
- Game-thread only (Deadworks hooks and commands run on the game thread).
