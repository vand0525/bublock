# ArenaSpotsTests

Unit tests for `Modules/Arena/ArenaSpots` with an inline arena.

- `Parse` reads the name, both anchors, offsets, `bounds` and
  `active_lane`.
- `For`: an Amber anchor facing +y puts "right" at +x and "back" at -y;
  the Sapphire anchor facing -y mirrors it; slots wrap (3 → 0, -1 → 2);
  a spectator team gets no spot.
- `Contains` uses the box; without `bounds` everything is inside.
- A row with two numbers throws.
