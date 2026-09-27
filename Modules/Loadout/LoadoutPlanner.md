# LoadoutPlanner

Pure planning for a build: which items to grant and which upgrade bits to
set on each ability. No Deadworks calls.

## Operations

| Op | Behavior |
|---|---|
| `ItemOrder(categories, fallback, rng = null)` | The buy order. Walks the build's categories left to right: every item of a required category; one item of an optional category (random with `rng`, the first without), skipping items already chosen and banned items. With no categories, the flat `fallback` list without banned items or duplicates. |
| `ItemOrder(build, rng = null)` | Same, from `build.Categories` / `build.Items` |
| `FirstSlots(order, componentsOf, slots = 9, exists = null)` | Walks the buy order ("buy left to right"). Skips items already owned, banned items, and items `exists` rejects. Buying an item removes its direct components from the owned list, like the shop's upgrade. Stops as soon as `slots` items are owned. Returns the owned items in purchase order. |
| `Value(items, costOf)` | Sum of item costs |
| `CapValue(items, costOf, cap, minItems = 6)` | Removes the most expensive item (the later one on a tie) until the total is at most `cap` or only `minItems` remain. Returns `(Kept, Removed)` |
| `AbilityBits(steps)` | Per ability, in first-seen order: unlocked plus the number of upgrades (capped at 3; an upgrade implies unlock; unknown kinds ignored). Returns `(Ability, Bits)`. |
| `BitsFor(upgrades)` | Bit 0 = unlocked, then one bit per tier (`0b1`, `0b11`, `0b111`); 3 or more upgrades give `FullyUpgradedBits` (`0b11111`, the value the Deadworks Deathmatch example uses to max an ability). |

## Constants

`DefaultSlots` = 9, `MaxUpgrades` = 3, `FullyUpgradedBits` = `0b11111`,
`DefaultMinItems` = 6, `Banned` = Monster Rounds (`upgrade_non_player_bonus`),
Cultist Sacrifice (`upgrade_non_player_bonus_sacrifice`) and Golden Goose Egg
(`upgrade_goose_egg`); the fetch script drops them too,
this is a guard for old or hand-edited data.

## Deadworks notes

- Only "bit 0 = unlocked" (`CCitadelBaseAbility.IsUnlocked`) and the
  `0b11111` maximum are known-good. Partial tiers (`0b11`, `0b111`) are
  unverified in game; builds applied in full nearly always reach 3 upgrades.
