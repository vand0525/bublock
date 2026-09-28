# LoadoutPlannerTests

Unit tests for `Modules/Loadout/LoadoutPlanner` (pure).

- `FirstSlots`:
  - takes items in build order, up to the slot count (9 by default)
  - skips duplicates
  - an upgrade replaces its direct component, which frees a slot for the
    next item (only direct components are removed, like the shop)
  - stops as soon as the slots are full, even if the next item is an upgrade
  - skips items the `exists` check rejects
  - skips banned items (Monster Rounds, Golden Goose Egg); the next item
    fills the slot
- `ItemOrder`:
  - all required-category items plus one random item per optional group, in
    category order (20 seeds)
  - without an rng, the first optional item
  - optional groups skip items already chosen and banned items; a group
    with nothing left adds nothing
  - no categories: the flat item list without banned items or duplicates
- `CapValue`: keeps a loadout under the cap unchanged; removes the most
  expensive items (the later one on a tie) until the total fits; never goes
  below the minimum item count.
- `BitsFor`: bit 0 = unlocked, one extra bit per tier; 3 or more upgrades
  give `FullyUpgradedBits` (`0b11111`).
- `AbilityBits`: keeps first-seen ability order, caps upgrades at 3, treats
  an upgrade without an unlock as unlocked, ignores unknown step kinds.
- `AbilityPrefix`:
  - with the level-36 budget (4 unlocks, 32 points) a full 16-step order
    matches `AbilityBits` and spends everything
  - stops when the next tier costs 5 and only 4 points remain
  - never skips ahead to a cheaper later step
  - stops when unlocks run out
  - an upgrade on a locked ability pays 1 unlock and the tier cost
  - repeat unlocks, upgrades past 3 and unknown kinds cost nothing
- `TryParseCap`: accepts whole numbers from 1,000 to 200,000 (edges
  included, spaces trimmed) and `default` in any case (20,000); rejects
  999, 200,001, negatives, `15,000`, decimals, words, blank and null.
