# MirrorPickTests

Unit tests for `RiftRoulette/Mirror/MirrorPick` (pure, seeded `Random`).

- Unpinned: one pool hero and a build index in range (50 seeds), never
  last round's hero, only heroes with builds; null when none have builds.
- Unpinned over 40 chained rounds: the hero switches every round and every
  pool hero comes up (guards that nothing, such as Random mode's priority
  heroes, pins the mirror hero).
- Hero pin: always that hero, with the build still rolled (more than one
  index seen over 50 seeds).
- Hero and build pins: always that hero and that slot (slot 2 is index 1).
- `TryPinSlot`: refused with no hero pinned (pins unchanged) and for slots
  outside `1..buildCount`.
- A slot with no hero (never produced by `TryPinSlot`) is ignored and the
  build rolls.
- `PinHero` keeps the slot only when the new hero has it; `ClearSlot`
  keeps the hero.
- `Share`: every fighter (duplicates dropped) gets the exact same choice.
