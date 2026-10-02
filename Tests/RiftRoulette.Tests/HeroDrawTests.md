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
- Priority heroes (`mustInclude`): one in the pool is assigned in every
  draw (200 seeds); one missing from the pool is skipped; with
  reservations it goes to the non-reserved player; with more priority
  heroes than players each player gets a different one; it never goes back
  to last round's holder when another player can take it; an empty list
  gives exactly the plain draw.
