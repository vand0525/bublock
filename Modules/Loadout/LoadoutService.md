# LoadoutService

Applies a stored hero build to a live pawn, and swaps a player to a hero
and then applies a build. Since Stage 13g it also captures a player's exact
hero state (`LoadoutSnapshot`) and applies it to another pawn. Game-agnostic.

## Types

- `LoadoutOptions(Level = 36, Gold = 0, Slots = 9, MaxValue = 20000)`
- `LoadoutResult(ItemsAdded, ItemsFailed, Imbued, AbilitiesSet, AbilitiesMissing, Unknown, Value, ItemsCapped)`
- `DefaultMaxValue` = 20,000 souls: the most a loadout's items may be worth.

## Operations

### `Apply(pawn, build, options, catalog = Default, mode, rng = Random.Shared)`

Follows the Deadworks Deathmatch example's known-good order:

1. `pawn.ResetHero()`: wipes items and abilities.
2. `pawn.Level = options.Level`, then `ModifyCurrency(EGold, 0, ECheats,
   silent)` so health and damage recalculate for the level.
3. Abilities: for each `LoadoutPlanner.AbilityBits` entry,
   `AbilityComponent.FindAbilityByName`, then `UpgradeBits |= bits`.
   Abilities missing on the hero are counted and logged at Trace.
4. Items: `LoadoutPlanner.ItemOrder(build, rng)` (one random pick per
   optional group), then `FirstSlots(order, catalog.ComponentsOf,
   options.Slots, ItemInfo.Exists)`, then `CapValue(slots, catalog.CostOf,
   options.MaxValue)` (drops the most expensive items while over the cap,
   keeping at least 6), then `pawn.AddItem(name)` for each kept item.
   Unknown items are collected. For imbuable items (`ItemInfo.CanBeImbued`),
   it imbues into the build's target ability (`build.Imbues`), else into the
   first signature slot that accepts it (`CanImbue`).
5. `SetCurrency(EGold, options.Gold)`, then `Heal(GetMaxHealth())`.

Returns a `LoadoutResult`. Logs `Loadout over cap, removed ...` (with value,
cap, and baseline) when items were capped, one Information line per loadout
(with `PlayerRef`, value, baseline, capped count), and a Warning when any
item failed or was unknown.

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
  `ModifyCurrency` level-ups (see the Deathmatch example comment).
