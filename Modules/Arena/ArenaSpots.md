# ArenaSpots

Where a game type's players spawn: one anchor per team plus a per-slot
offset, read from a JSON asset the game type embeds. Pure apart from
reading the embedded resource; unit tested in `Tests/Modules.Tests`.

## Asset format (a game type's `arena.json`)

```json
{
  "name": "mid lane brawl",
  "sapphire": { "position": [x, y, z], "angle": [pitch, yaw, roll] },
  "amber":    { "position": [x, y, z], "angle": [pitch, yaw, roll] },
  "offsets":  [[forward, right, up], ...],
  "bounds":   { "min": [x, y, z], "max": [x, y, z] },
  "active_lane": 4
}
```

`bounds` (optional): the containment box; `ArenaService.ReturnStrays` sends
living players outside it back to their spot. `active_lane` (optional): the
game's single-lane setting (`citadel_active_lane`, 0 = all lanes; lane
numbers from the map's `lane_marker_path` entities: 1 west, 4 middle, 6
east on `dl_midtown`), applied by the game type.

Offsets are turned by the anchor's yaw (`MovementLocation.Offset`). Extra
keys (like `source`) are ignored. Check new spots against the map mesh
with `scripts/check-arena.py <arena.json>` before use (`--probe X Y`
lists floor heights while designing).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Load(assembly, resourceName)` | Reads and parses an embedded resource; throws when it is missing | `ArenaSpots` |
| `Parse(json)` | The format above; throws on a missing key or a row that is not 3 numbers | `ArenaSpots` |
| `For(team, slot)` | Team anchor + `Offsets[Index(slot)]`, named `<anchor>#<slot>` | location, or null for a team that is not Amber / Sapphire |
| `Index(slot, count)` | `slot` wrapped into `0..count-1` (negative too) | int |
| `Contains(point)` / `Bounds` / `ActiveLane` | Inside the box (true with no box) / the box / the lane setting | — |

## Invariants

- Never teleport a group to one point: every player gets their slot's
  spot (`.rules` §0.2).
- Slots beyond the offset count wrap around.
