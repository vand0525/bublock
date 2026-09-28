# Scoreboard

Points per player for one match. Pure; unit tested in `Tests/Modules.Tests`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Add(steamId, points = 1)` | Adds points | the player's new total |
| `PointsOf(steamId)` | Points, 0 when none | int |
| `PlaceOf(steamId)` | 1 + players with more points (ties share a place; unknown = last) | int |
| `Standings()` | Most points first, then by Steam ID | list of `(SteamId, Points)` |
| `Forget(steamId)` / `Clear()` | Drop one player / everyone | — |
| `Count`, `Points` | Rows / read-only map | — |

## Invariants

- Players with no points are absent, so `Standings()` lists only scorers.
- What a point is (a kill, a goal, a capture) is the game type's call.
