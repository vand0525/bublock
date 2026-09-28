# LoadoutPlanner

Pure planning for a build: which items to grant and which upgrade bits to
set on each ability. No Deadworks calls.

## Operations

| Op | Behavior |
|---|---|
| `ItemOrder(categories, fallback, rng = null)` | The buy order. Walks the build's categories left to right: every item of a required category; one item of an optional category (random with `rng`, the first without), skipping items already chosen and banned items. With no categories, the flat `fallback` list without banned items or duplicates. |
| `ItemOrder(build, rng = null)` | Same, from `build.Categories` / `build.Items` |
| `OptionalItems(categories)` / `OptionalItems(build)` | Every item of the build's optional categories, in order, without banned items or duplicates (the fill pool) |
| `Plan(order, componentsOf, costOf, budget, slots = 12, sellPriorityOf = null, exists = null, fillers = null, upgradesOf = null)` | Three passes with `budget` souls of net worth and `slots` universal slots. **Buy:** shops the buy order left to right. Skips items already owned, banned items, and items `exists` rejects. An upgrade consumes its owned direct components (their cost counts toward it, they free their slots). With every slot full it picks one item to sell (`SellCandidate`); none means the item is skipped. The buy happens only if the held items' value afterwards is at most `budget` (a sale takes the sold item's full cost off); otherwise the item is skipped, nothing is sold, and the walk goes on, so a later cheaper item can still fit. **Fill:** while a slot is empty, `fillers` (the optional items), most expensive first (ties in pool order), each bought if it fits the budget; never sells. **Upgrade** (only with `upgradesOf`): repeatedly, for each held item in order, buys its priciest affordable upgrade (an item it is a direct component of, pricier than it, not owned or banned); the component is consumed, so no slot is needed. Repeats until a whole pass buys nothing, so T1 goes to T2 to T3 while the budget lasts. Returns `ShopPlan(Items, Value, Bought, Sold, Skipped, Filled, Upgraded)`: held items in purchase order, their value (never above `budget`), and the names bought (all passes), sold, skipped, filled and upgraded. |
| `SellCandidate(owned, keep, replacementCost, costOf, sellPriorityOf = null)` | The owned item to sell for a new one: not in `keep` (the new item's components) and cheaper than `replacementCost`. Order: highest sell priority first (only non-zero values count; builds mark items to sell late game, higher sells first), then cheapest, then earliest bought. Null when nothing qualifies |
| `Value(items, costOf)` | Sum of item costs |
| `TryParseCap(text, out souls)` | For `/loadout_cap`: `default` (any case) gives `DefaultCap`; otherwise a plain whole number (no sign, separators or decimals; spaces trimmed) from `MinCap` to `MaxCap`. False for anything else |
| `AbilityBits(steps)` | Per ability, in first-seen order: unlocked plus the number of upgrades (capped at 3; an upgrade implies unlock; unknown kinds ignored). Returns `(Ability, Bits)`. The whole order, no budget. |
| `AbilityPrefix(steps, unlocks, points)` | Walks the build's order and buys steps while both budgets last; stops at the first step that does not fit (never skips ahead). Unlocking an ability costs 1 unlock (an upgrade on a locked ability pays that unlock first); upgrade tiers cost `UpgradeCosts` 1, 2, 5 points. Repeat unlocks and upgrades past 3 cost nothing. Returns `AbilityPlan(Bits, StepsTaken, StepsTotal, UnlocksUsed, PointsUsed)`; `Bits` is `(Ability, BitsFor(tiers))` in first-seen order. |
| `BitsFor(upgrades)` | Bit 0 = unlocked, then one bit per tier (`0b1`, `0b11`, `0b111`); 3 or more upgrades give `FullyUpgradedBits` (`0b11111`, the value the Deadworks Deathmatch example uses to max an ability). |

## Constants

`DefaultSlots` = 12 (universal item slots with every flex slot open),
`MaxUpgrades` = 3, `UpgradeCosts` = 1, 2, 5 (the game's
tier costs; builds record them as `delta` -1/-2/-5), `FullyUpgradedBits` = `0b11111`,
`DefaultCap` = 20,000 (souls a loadout's items may
be worth unless an admin changes it), `MinCap` = 1,000, `MaxCap` = 200,000,
`DefaultCapWord` = `default`, `Banned` = Monster Rounds (`upgrade_non_player_bonus`),
Cultist Sacrifice (`upgrade_non_player_bonus_sacrifice`), Golden Goose Egg
(`upgrade_goose_egg`), Trophy Collector (`upgrade_trophy_collector`) and
Healing Rite (`upgrade_health_stimpak`); the fetch script drops them too,
this is a guard for old or hand-edited data.

## Invariants

- `Plan(...).Value` is at most the budget; there is no minimum item count
  (a 1,000 budget buys one 800 item).
- At most `slots` items are held; a sale only happens in the buy pass, for
  a pricier replacement that fits the budget.
- Fill and upgrade only add value up to the budget; at low caps they buy
  nothing. The upgrade pass never touches items that upgrade into nothing.

## Deadworks notes

- Only "bit 0 = unlocked" (`CCitadelBaseAbility.IsUnlocked`) and the
  `0b11111` maximum are known-good. Partial tiers (`0b11`, `0b111`) are
  unverified in game. Budgeted loadouts (`AbilityPrefix`) use them often:
  at a 20,000-soul kit only 21 of the 32 points are earned.
