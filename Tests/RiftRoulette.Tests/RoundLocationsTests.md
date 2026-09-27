# RoundLocationsTests

Unit tests for `RiftRoulette/Round/RoundLocations`.

- Green maps to `green_sapphire` / `green_amber`; Yellow maps to
  `yellow_sapphire` / `yellow_amber` (checked by location name).
- On both sides the Sapphire start has y > 0 and the Amber start y < 0,
  matching the team bases (team 3 at +y, team 2 at -y). Guards against the
  archive's swapped yellow starts.
- `WatchFor`: Green maps to `watch_green`, Yellow to `watch_yellow`; each
  spot is well above its team starts and at the same height as the old
  `draft` spot (the skybox floor).
