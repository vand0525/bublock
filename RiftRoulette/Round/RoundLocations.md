# RoundLocations

Which start locations each team uses for a rift side. Pure; unit tested in
`Tests/RiftRoulette.Tests`. Moved out of `RiftService.MoveTeamsToRift` in
Stage 11 (same mapping as the archive `/koth`).

## Operations

`StartsFor(side)` returns `(Sapphire, Amber)`:

| Side | Sapphire | Amber |
|---|---|---|
| Green | `green_sapphire` | `green_amber` |
| Yellow | `yellow_sapphire` | `yellow_amber` |

`WatchFor(side)` returns the watch spot above that rift: Green →
`watch_green`, Yellow → `watch_yellow` (Stage 13i, used by `WatchSpot`).

Values are the typed `RiftRouletteLocations` fields, so it never depends on
the Movement registry having been filled.
