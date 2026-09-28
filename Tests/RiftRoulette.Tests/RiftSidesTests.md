# RiftSidesTests

Unit tests for `RiftRoulette/Rift/RiftSide` (`RiftSides`).

- `TryParse` accepts `green` / `yellow` in any case and with surrounding
  spaces; rejects `middle`, empty, and null.
- `Other` flips Green and Yellow.
- `Name` returns `GREEN` / `YELLOW`.
- `Position` returns the rift vectors (7612, -0.000661, 444) and
  (-7560, 0, 424).
- `TryMatch` finds the side at each rift position and a few hundred units
  off it; rejects the map middle, a point 5000 units off green, and a point
  far past yellow.
