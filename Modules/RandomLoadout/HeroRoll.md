# HeroRoll

Picks a random hero for one player. Pure; unit tested in
`Tests/Modules.Tests`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Pick(pool, current, taken, rng)` | A random hero from `pool` that is not `current`, preferring heroes not in `taken` (heroes other players hold); when every other hero is taken, any hero but `current` | hero, or null when the pool has nothing but `current` |

## Invariants

- Never returns the player's current hero (a reroll always changes hero).
- Duplicates happen only when the pool is smaller than the lobby.
