# HeroDrawTests

Unit tests for `RandomMode/HeroDraw` (pure, seeded `Random`).

- Every player gets a hero from the pool, and no two players share one
  (checked over 50 seeds).
- No player gets the hero they had last round, even when the previous
  round used six of the six pool heroes.
- A one-hero pool repeats the previous hero instead of leaving the player
  out.
- More players than heroes: everyone is still assigned (duplicates allowed).
- An empty pool assigns nobody.
- Fixed heroes (reservations): the player gets theirs even if it was last
  round's hero, nobody else draws it, heroes stay unique, and a fixed entry
  for someone not playing is ignored (50 seeds).
- An empty fixed map gives exactly the plain draw for the same seed.
