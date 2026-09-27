# StreakBoard

Best streak per player for one 1v1 match. Pure; unit tested in
`Tests/RiftRoulette.Tests`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Record(steamId, streak)` | Stores `streak` when it beats the player's best | true when the best changed |
| `BestOf(steamId)` | Best streak, 0 when none | int |
| `Best` | Steam ID to best streak (unordered) | read-only map |
| `Forget(steamId)` | Drops the player | — |
| `Clear()` | Drops everyone | — |

## Invariants

- A best never goes down: a later, shorter streak leaves it unchanged.
- Players with no win are absent (`BestOf` is 0).
- Ordering for display is `StatsBoardText.StreakBoard`.
