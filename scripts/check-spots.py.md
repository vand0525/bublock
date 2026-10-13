# check-spots.py

Checks every per-slot teleport spot against the `dl_midtown` world mesh.
Read-only: prints a table per anchor and exits 1 if any spot fails.

## Usage

```bash
python3 Bublock/scripts/check-spots.py                 # Rift Roulette
python3 Bublock/scripts/check-spots.py --mode crystal  # Crystal Crackers pit round
```

| Mode | Spots | Anchors | Groups |
|---|---|---|---|
| `rift` (default) | `RiftRoulette/Round/Data/spots.json` | `RiftRoulette/Locations/RiftRouletteLocations.cs` | `watch` at `watch_green` / `watch_yellow` / `watch_center`; `fight` at the six `green_*` / `yellow_*` / `center_*` starts |
| `crystal` | `CrystalCrackers/Pit/Data/spots.json` | `CrystalCrackers/Pit/PitLocations.cs` | `watch` at `pit_watch`; `pit` at `pit_sapphire` / `pit_amber`; `lane` at `lane_sapphire` / `lane_amber` |

Run after changing an anchor or a `spots.json`; every spot must pass,
then walk them in game.

## Behavior

Anchors are parsed from the `new("name", new Vector3(...), new Vector3(...))`
lines of the locations file; each spot is the anchor plus the slot's
(forward, right, up) offset in the anchor's yaw frame (as
`MovementLocation.Offset`). Per spot:

1. **Floor:** a mesh hit within 64 below (and within 16 of the spot's
   height). Skipped for watch spots: they stand on the skybox floor, which
   is not in the render mesh.
2. **Body:** no geometry inside a hero cylinder (radius 40, 18 to 80
   above the floor).
3. **Line:** nothing between the anchor and the spot at 48 above both
   (nobody placed behind a wall).

## Needs

`RiftRoulette/reference/maps/dl_midtown/world.bin.gz` and `world.json`
(the bin is not in git; `resources.md` "dl_midtown map dump" has the
refresh steps).
