# PitSpots

Per-slot teleport spots for the pit round: every player slot (0-12) has its
own spot up top, in the pit and in the lane, so nobody stands on anyone
else. Each spot is a `PitLocations` anchor plus that slot's offset, applied
with `MovementLocation.Offset` in the anchor's own frame (forward = anchor
yaw, right, up). Amber anchors face half a turn from Sapphire, so Amber
spots mirror Sapphire's. A copy of Rift Roulette's `Round/SlotSpots` until
the lobby extraction stage moves slot spots into a module.

## Data

`Pit/Data/spots.json`, embedded as `CrystalCrackers.Pit.spots.json` and
read once on first use:

```json
{ "watch": [[forward, right, up], ...13], "pit": [...13], "lane": [...13] }
```

- `watch`: Rift Roulette's watch table (two rows, 100 apart).
- `pit`: three rows on the flat end of the pit (forward 45, -25, -90)
  plus two spots 120 forward, clear of the raised block and the ramp (the
  first layout clipped a wall at two slots). The end is small: spots are
  at least 65 apart (Rift Roulette's are 100).
- `lane`: Rift Roulette's fight table (three rows, 100 apart).

All 65 spots pass `scripts/check-spots.py --mode crystal` (2026-10-13).

## Operations

| Op | Returns |
|---|---|
| `Offsets` | The embedded table (throws if missing or malformed) |
| `Watch(slot)` | Spot up top |
| `Pit(team, slot)` / `Lane(team, slot)` | Spot at that team's pit end / lane start; null for a team other than 2 or 3 |
| `At(anchor, offsets, slot)` | Offset for `Index(slot)`; a slot outside the table logs one Warning per slot in `pit-*.log` |
| `Index(slot, count)` | `slot` wrapped into `0..count-1` |
| `Parse(json)` | Needs non-empty `watch`, `pit` and `lane` lists of `[forward, right, up]`; throws `InvalidOperationException` otherwise |

## Invariants

- Keyed by `player.Slot`, never by team or join order.
- Pure apart from the one-time Warning; tested in `PitSpotsTests`.
