# PitSpotsTests

Unit tests for `CrystalCrackers/Pit/PitSpots` with the committed
`Pit/Data/spots.json`.

- Every table (`watch`, `pit`, `lane`) has 13 spots.
- Pit spots stand on the pit floor (z -768), on their own team's end
  (Sapphire y above 300, Amber below -300), within 700 of the center.
- Amber pit spots mirror Sapphire's (x and y negated).
- Pit spots are at least 65 apart.
- Lane slot 0 is Rift Roulette's mid spawn: (0, 2000, 384) for Sapphire,
  (0, -2000, 384) for Amber.
- Teams other than 2 and 3 get no pit or lane spot.
- `Index` wraps slots into the table.
- `Parse` throws without all three tables or with a short offset.

Wall clearance is checked against the map mesh by
`scripts/check-spots.py --mode crystal`, not here.
