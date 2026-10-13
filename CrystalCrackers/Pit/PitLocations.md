# PitLocations

Named anchors for the pit round on `dl_midtown`, registered with
`MovementService.Locations` (`RegisterAll`, from `PitRoundPlugin.OnLoad`)
so `/mv_tp <name>` reaches them.

| Name | Position | Angle | Use |
|---|---|---|---|
| `pit_watch` | (0, 0, 1536.06) | (-23, 45, 0) | Up top: the sky floor right above the pit (same spot as Rift Roulette's `watch_center`) |
| `pit_sapphire` | (60, 500, -768) | yaw -90 | Pit team start when Sapphire holds the pit: the flat pit end toward Sapphire's base, facing the boss |
| `pit_amber` | (-60, -500, -768) | yaw 90 | The same for Amber (the pit floor is rotationally symmetric) |
| `lane_sapphire` | (0, 2000, 384) | yaw -90 | Lane team start when Sapphire attacks: mid lane on Sapphire's side, facing mid (Rift Roulette's `center_sapphire`) |
| `lane_amber` | (0, -2000, 384) | yaw 90 | The same for Amber (`center_amber`) |

`PitFor(team)` / `LaneFor(team)`: the anchor for team 2 (Amber) or 3
(Sapphire); null for any other team.

## Map notes (world mesh, 2026-10-13)

- The pit floor is z -768, about x -300 to 300 and y -600 to 600, ringed
  by a step at -640. The flat team ends are y ±450 to ±600; ramps rise on
  one side of each end, and a raised block sits at (0, ±400) between the
  ends and the boss (z -759 at the center).
- `pit_watch` stands on the sky clip floor (z 1536), so
  `citadel_check_walk_on_skyclip` must stay 0 (`PitRoundPlugin` sets it
  on load and map start).
- Every per-slot spot passes `scripts/check-spots.py --mode crystal`.
