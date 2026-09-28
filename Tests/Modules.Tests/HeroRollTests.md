# HeroRollTests

Unit tests for `Modules/RandomLoadout/HeroRoll` with a three-hero pool.

- The current hero is never drawn (50 seeds).
- A hero nobody else holds is preferred (the only free one is always
  drawn).
- When every other hero is taken, a taken one is drawn; a pool of only the
  current hero gives null; no current hero draws anything.
