# RoundHeroesTests

Unit tests for `RiftRoulette/Round/RoundHeroes`.

- `Set` records the round hero (`Has`, `TryGet`, `All`).
- `Set` again replaces the player's hero (one entry).
- Two players may hold the same hero (Mirror mode).
- `Remove` drops only that player and returns their hero; without an entry
  it returns false.
- `Clear` empties everything.
