# PriorityHeroesTests

Unit tests for `RiftRoulette/RandomMode/PriorityHeroes`.

- `List` holds exactly Rat King, Baba and Solomon (`Heroes.Chessmaster`).
- `Guaranteed` is false at 2 and 7 fighters, true at 8 and 12.
- `InPool` keeps only listed heroes that are in the pool (have builds), in
  pool order; an empty list gives nothing.
- `RerollPool` never offers a failed hero, prefers heroes no other fighter
  holds, falls back to held ones, and is empty when only failed heroes are
  left.
- `RefusedLine` names the hero and the action (`reserved` / `banned`).
