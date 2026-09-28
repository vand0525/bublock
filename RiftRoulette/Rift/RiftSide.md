# RiftSide

The two rift sides in rotation and their spawn positions. Pure; unit tested
in `Tests/RiftRoulette.Tests`.

## Values

| Member | Value |
|---|---|
| `GreenPosition` | (7612, -0.000661, 444) |
| `YellowPosition` | (-7560, 0, 424) |
| `MiddlePosition` | (0, 0, 0). Known but never used; not in rotation (inventory §1.8) |

## Operations

- `Position(side)`: the green or yellow position.
- `Other(side)`: the opposite side.
- `Name(side)`: `GREEN` / `YELLOW` (the spelling used in logs).
- `TryMatch(position, out side)`: the side whose position is within
  `MatchDistance` (1000 units) of `position`; false (side Green) when
  neither is. The rifts are about 15,000 units apart. Used to find the side
  of a spawner on the map.
- `Nearest(position)`: the closer of the two sides, with no distance limit
  (ties go to Green). Used for a leftover `citadel_koth_cashin` whose
  position is not within `MatchDistance`.
- `TryParse(name, out side)`: `green` or `yellow`, ignoring case and
  surrounding spaces. Anything else (including `middle`) returns false with
  `side` set to Green.

## Invariants

- Only two sides rotate; Middle is never returned by `Position`.
