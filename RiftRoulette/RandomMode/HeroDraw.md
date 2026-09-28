# HeroDraw

Pure random hero draw for Random mode. This is the single place to replace
when heroes later come from a real match ID.

## Operations

`Draw(players, pool, previous, rng, fixedHeroes = null)` returns Steam ID to hero:

- `fixedHeroes` (hero reservations, `HeroReservations.Take`): each listed
  player who is in `players` gets that hero, with no repeat check. Their
  heroes leave the pool for everyone else (unless that would empty it).
  Players not in `players` are ignored. Null or empty: the draw below runs
  unchanged over every player.
- Shuffles the pool and the players, then gives each player the first
  remaining hero that is not the one they had last round (`previous`).
  Heroes are unique while the pool lasts.
- If a player's only remaining hero is their previous one, the whole draw
  is retried (up to `Attempts` = 20). The first draw with no repeats wins;
  otherwise the last draw is returned.
- A player can repeat only when the pool leaves no other choice (for
  example, a one-hero pool).
- More players than heroes: extra players get a random pool hero
  (duplicates).
- Empty pool: nobody is assigned.

## Invariants

- Deterministic for a given `Random` seed (unit tested with seeds).
- The real pool (`HeroBuildCatalog.Default.Heroes`, 38 heroes) is far larger
  than a lobby (12), so live draws are unique and never repeat.
