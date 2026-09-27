# LoadoutService

Applies a stored hero build to a live pawn, and swaps a player to a hero
and then applies a build. Since Stage 13g it also captures a player's exact
hero state (`LoadoutSnapshot`) and applies it to another pawn. Game-agnostic.

## Types

- `LoadoutOptions(Gold = 0, Slots = 9, MaxValue = 20000)`. There is no
  level option: the level follows the item value.
- `LoadoutResult(ItemsAdded, ItemsFailed, Imbued, AbilitiesSet, AbilitiesMissing, Unknown, Value, ItemsCapped, Progression, AbilityPlan)`
- `DefaultMaxValue` = 20,000 souls: the most a loadout's items may be worth.

## Operations

### `Apply(pawn, build, options, catalog = Default, mode, rng = Random.Shared)`

Gives a hero the power a real player of that build has at the same net
worth. Plans first, then follows the Deadworks Deathmatch example's
known-good order:

1. Plan the items: `LoadoutPlanner.ItemOrder(build, rng)` (one random pick
   per optional group), then `FirstSlots(order, catalog.ComponentsOf,
   options.Slots, ItemInfo.Exists)`, then `CapValue(slots, catalog.CostOf,
   options.MaxValue)` (drops the most expensive items while over the cap,
   keeping at least 6). Unknown items are collected. `value` is the kept
   items' soul cost.
2. Plan the power: `Progression.ForSouls(value)` gives the level, boons,
   unlocks and ability points at that net worth; `AbilityPrefix(build.Abilities,
   unlocks, points)` gives the ranks the build's order has bought by then.
3. `pawn.ResetHero()`: wipes items and abilities.
4. `pawn.Level = progression.Level`, then `ModifyCurrency(EGold, 0, ECheats,
   silent)` so health and damage recalculate for the level (the boons).
5. Abilities: for each prefix entry, `AbilityComponent.FindAbilityByName`,
   then `UpgradeBits = bits`. Abilities missing on the hero are counted and
   logged at Trace.
6. Items: `pawn.AddItem(name)` for each kept item. For imbuable items
   (`ItemInfo.CanBeImbued`), it imbues into the build's target ability
   (`build.Imbues`), else into the first signature slot that accepts it
   (`CanImbue`).
7. `SetCurrency(EGold, options.Gold)`, `SetCurrency(EAbilityPoints, 0)`,
   `SetCurrency(EAbilityUnlocks, 0)`: the ranks are the build's, and the
   points left over after the prefix stopped are not handed out. Then
   `Heal(GetMaxHealth())`.

Returns a `LoadoutResult`. Logs `Loadout over cap, removed ...` (with value,
cap, and baseline) when items were capped, one Information line per loadout
(`Loadout applied`, with `PlayerRef`, value, baseline, capped count,
`Level`, `Boons`, `Unlocks`, `Points`, `PointsLeft`, `Steps` / `StepsTotal`
and `Ranks` as `ability:bits`), and a Warning when any item failed or was
unknown.

### `Swap(player, hero, build, timer, options, mode, applied)`

- Returns false (Debug line) when the player has no live pawn: never
  `SelectHero` while dead.
- Otherwise `player.SelectHero(hero)`, then after `SwapDelaySeconds` (1 s,
  as in the Deathmatch example; hero loading is async) it finds the player
  again by Steam ID, then `Apply`, then invokes `applied(player, result)`.
- Skips (Information) when the player left or is dead. When the pawn's
  `HeroID` is not `hero` yet, it calls `SelectHero(hero)` again and waits
  another `SwapDelaySeconds`, up to `SwapAttempts` (3) tries in all
  (Information `Hero did not change, selecting again ... Attempt=`); after
  the last try it skips with a Warning (`Attempts=`). A `SelectHero` issued
  in the same frame as another one (for example on a join) can be lost.
  Exceptions in `Apply` are logged at Error.
- The delayed part is the private `AfterSwap(steamId, hero, timer, log,
  apply, attempt = 1)`, shared with `SwapSnapshot`.

### `Capture(pawn, source = "")`

Reads, without changing anything: `HeroID`, `Level`,
`GetCurrency(EAbilityPoints)`, `GetCurrency(EAbilityUnlocks)`, then walks
`AbilityComponent.Abilities` in order: `IsItem` entries become
`SnapshotItem(name, ImbuedAbilities)`, `IsSignature` entries become
`SnapshotAbility(name, slot, UpgradeBits)`. Returns a `LoadoutSnapshot`.

### `ApplySnapshot(pawn, snapshot, gold = 0, mode)`

Same Deathmatch order as `Apply`, but exact (no cap, no banned-item filter,
no random picks):

1. `ResetHero()`.
2. `Level = snapshot.Level`, then `ModifyCurrency(EGold, 0, ECheats,
   silent)`.
3. For each snapshot ability: `FindAbilityByName`, falling back to
   `GetAbilityBySlot`; `UpgradeBits = bits` (set, not OR). Missing ones are
   counted (Trace).
4. `SetCurrency(EAbilityPoints)` and `SetCurrency(EAbilityUnlocks)` to the
   snapshot values, so unspent points match the source.
5. `AddItem` for each item in order (failures counted, Trace); for each
   imbue target name, `FindAbilityByName`, then `ImbueItem(item, slot)`.
6. `SetCurrency(EGold, gold)`, then `Heal(GetMaxHealth())`.

Logs one Information line (`Snapshot applied ...` with `PlayerRef`) and a
Warning when an item failed or an ability was missing. Returns a
`SnapshotResult`.

### `SwapSnapshot(player, snapshot, timer, gold = 0, mode, applied)`

- No live pawn: false (Debug line).
- Already the snapshot hero: `ApplySnapshot` now (no `SelectHero`).
- Otherwise `SelectHero(snapshot.Hero)`, then after 1 s (`AfterSwap`, same
  checks and retries as `Swap`) `ApplySnapshot`, then `applied(player, result)`.

## Logs

`Loadout` feature log (`loadout-YYYYMMDD.log`).

## Deadworks notes

- `ResetHero` triggers the game's starting-souls grant; gold is set last so
  `options.Gold` wins.
- `AddItem` grants the item as owned, for free. It returns null when the
  game refuses (for example, category slots full).
- Setting `Level` directly avoids the per-level UI events of
  `ModifyCurrency` level-ups (see the Deathmatch example comment). It also
  grants no ability points, and `UpgradeBits` deducts none, so the wallets
  are set explicitly.
- Partial masks (`0b11`, `0b111`) are unverified in game. Budgeted
  loadouts use them; the `Ranks=` field of `Loadout applied` is what to
  check against the hero's ability panel.
