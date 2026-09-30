# RiftSide

The three rift sides in rotation and their spawn positions. Pure; unit
tested in `Tests/RiftRoulette.Tests`.

## Values

| Member | Value |
|---|---|
| `GreenPosition` | (7612, -0.000661, 444) |
| `YellowPosition` | (-7560, 0, 424) |
| `MiddlePosition` | (0, 0, 448). Approx side-rift KOTH height; no map `info_koth_spawn_location` (forced spawn) |

## Operations

- `All`: Green, Yellow, Center.
- `Position(side)`: the side's spawn position.
- `Other(side)`: Green ↔ Yellow (binary; used for stream framing mirror).
- `NextInRotation(spawned, middleEnabled)`: with mid on, Green → Yellow →
  Center → Green; with mid off, Green ↔ Yellow (Center advances to Green).
- `Name(side)`: `GREEN` / `YELLOW` / `CENTER` (the spelling used in logs).
- `TryMatch(position, out side)`: the side whose position is within
  `MatchDistance` (1000 units) of `position`; false (side Green) when
  none match.
- `Nearest(position)`: the closest of the three sides (ties keep the first
  found among nearer distances).
- `TryParse(name, out side)`: `green`, `yellow`, `center` / `middle` /
  `mid`, ignoring case and surrounding spaces.

## Invariants

- Mid spawn Z is an estimate; tune after a live forced spawn.
