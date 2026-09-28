# DraftStateTests

Unit tests for `RiftRoulette/Draft/DraftState` (static pick data).

- The constructor calls `DraftState.Clear()` so each test starts empty
  (xUnit creates a new instance per test and runs one class's tests one at
  a time).
- `Add` records the pick and marks the hero selected.
- `Release` removes the pick, frees the hero, and leaves other picks alone.
- `Release` for a player without a pick returns false.
- A second `Add` for the same player throws `ArgumentException`
  (`Dictionary.Add`).
- `Clear` empties both collections.
