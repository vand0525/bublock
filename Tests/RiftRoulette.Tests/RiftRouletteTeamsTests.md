# RiftRouletteTeamsTests

Unit tests for `RiftRoulette/Lobby/RiftRouletteTeams`.

- `TryParse` maps `sapphire` → 3 and `amber` → 2, ignoring case.
- `TryParse` rejects empty, unknown, and numeric names (returns false,
  team 0).
- `Name` maps 3 → `Sapphire`, 2 → `Amber`, anything else → `Team<n>`.
