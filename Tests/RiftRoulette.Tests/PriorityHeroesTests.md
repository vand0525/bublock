# PriorityHeroesTests

Unit tests for `RiftRoulette/RandomMode/PriorityHeroes`.

- `List` holds exactly Rat King and Baba.
- `InPool` keeps only listed heroes that are in the pool (have builds), in
  pool order; an empty list gives nothing.
- `RerollPool` never offers a failed hero, prefers heroes no other fighter
  holds, falls back to held ones, and is empty when only failed heroes are
  left.
- `RefusedLine` names the hero and the action (`reserved` / `banned`).
