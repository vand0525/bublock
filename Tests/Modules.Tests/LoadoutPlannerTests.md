# LoadoutPlannerTests

Unit tests for `Modules/Loadout/LoadoutPlanner` (pure).

- `Plan`:
  - fills 12 slots (the default) in build order; later same-price items
    are skipped
  - a 1,000 budget buys one 800 item (no minimum item count)
  - the value never goes over the budget (budgets 1,000 to 60,000)
  - an unaffordable item is skipped and a later cheaper one is bought
  - an upgrade costs the difference and frees its component's slot, also
    with every slot full (no sale); only direct components are consumed
  - with full slots it sells the cheapest, earliest item for a pricier one
  - no sale for an item that is not pricier, or when the replacement does
    not fit the budget
  - marked sell-priority items sell first, highest number first
  - skips duplicates, items `exists` rejects, and banned items including
    Healing Rite
  - fill: empty slots take the most expensive optional items first, the
    priciest one that fits the budget, and never sell
  - upgrade pass: T1 goes to T2 to T3 (`Upgraded` lists each step), stops
    at the budget, and runs after the fill
- `OptionalItems`: only optional-group items, no banned items or duplicates.
- `ItemOrder`:
  - all required-category items plus one random item per optional group, in
    category order (20 seeds)
  - without an rng, the first optional item
  - optional groups skip items already chosen and banned items; a group
    with nothing left adds nothing
  - no categories: the flat item list without banned items or duplicates
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
