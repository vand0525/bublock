# WatchSpotRule

Which rift the watch spot sits above (Stage 13i). Pure; unit tested in
`Tests/RiftRoulette.Tests`.

`SideFor(riftRunning, currentSide, nextSide)`:

- While a rift round runs and `currentSide` is known: `currentSide` (the
  rift people are still fighting on).
- Otherwise: `nextSide` (intermission, no match, lobby).

Why it's needed: `RiftService.NextSide` already flips to the other side
when the rift spawns, mid-round. Using `NextSide` alone would move players
who die mid-round above the wrong rift.
