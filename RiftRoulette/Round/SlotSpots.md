# SlotSpots

Per-slot teleport spots: every player slot (0-12) has its own watch-spot
position and its own rift start position, so nobody stands on anyone else.
Each spot is an anchor (`RiftRouletteLocations`) plus that slot's offset,
applied with `MovementLocation.Offset` in the anchor's own frame
(forward = anchor yaw, right, up). Moving an anchor moves the whole group;
editing one offset moves one slot. Yellow anchors face half a turn from
green, so yellow spots mirror green automatically.

## Data

`Round/Data/spots.json`, embedded in the DLL as
`RiftRoulette.Round.spots.json` and read once on first use:

```json
{ "watch": [[forward, right, up], ...13], "fight": [[forward, right, up], ...13] }
```

Default layouts (checked against the map by `scripts/check-spots.py`,
2026-09-27: all 78 spots pass):

- `watch`: two rows facing the welcome board. Slots 0-6 at forward 0,
  right 0, +100, -100, +200, -200, +300, -300; slots 7-12 at forward -100,
  right +50, -50, +150, -150, +250, -250 (back row further from the board).
- `fight`: three rows, at most 200 sideways because wider rows clipped
  buildings at the green and yellow starts. Slots 0-4 at forward 0,
  right 0, +100, -100, +200, -200; slots 5-8 at forward -100, right +50,
  -50, +150, -150; slots 9-12 at forward -200, right 0, +100, -100, +200
  (rows further back are further from the rift).

Neighbours are at least 100 units apart (about 112 across rows). Up is 0
everywhere, so the watch spots stay on the skybox floor and `WatchGuard`
keeps using the anchor height.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Offsets` | The embedded table (throws if the resource is missing or malformed) | `SpotOffsets(Watch, Fight)` |
| `Watch(anchor, slot)` / `Fight(anchor, slot)` | `At` with the watch / fight list | location named `<anchor>#<slot>`, same angle as the anchor |
| `At(anchor, offsets, slot)` | Offset for `Index(slot)`; a slot outside the table logs one Warning per slot in `Round` (`Slot outside the spot table`) | `MovementLocation` |
| `Index(slot, count)` | `slot` wrapped into `0..count-1` (13 becomes 0, -1 becomes 12) | int |
| `Parse(json)` | Needs non-empty `watch` and `fight` lists of `[forward, right, up]`; throws `InvalidOperationException` otherwise | `SpotOffsets` |

## Invariants

- Keyed by `player.Slot`, never by team or join order. Teammates never share
  a slot, and the two team anchors are far apart, so no two fighters get the
  same spot.
- Pure apart from the one-time Warning; unit tested in `SlotSpotsTests`
  (spacing, height, angle, yellow mirror, team halves, wrap).
- `scripts/check-spots.py` checks every spot against the map mesh offline.
