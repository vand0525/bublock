# LoadoutService

Applies a stored hero build to a live pawn, and swaps a player to a hero
and then applies a build. It also captures a player's exact
hero state (`LoadoutSnapshot`) and applies it to another pawn. Game-agnostic.

## Types

- `LoadoutOptions(Gold = 0, Slots = null, MaxValue = null, ItemOrder = null)`. There is no
  level option: the level follows the cap. `MaxValue` null means
  the current `LoadoutService.MaxValue`; `Slots` null means
  `ItemSlots.ForSouls(cap)`. A caller's own value wins (none pass one
  today). `ItemOrder` null means `LoadoutPlanner.ItemOrder(build, rng)`
  (one random item per optional group, drawn on every `Apply`); a caller
  that gives several players the same build passes one list drawn once so
  every plan comes out the same (mirror mode).
- `LoadoutResult(ItemsAdded, ItemsFailed, Imbued, AbilitiesSet, AbilitiesMissing, Unknown, Value, Cap, ItemsSold, ItemsSkipped, Progression, AbilityPlan)`.
  `Value` is the items' soul cost (at most `Cap`); `Progression` comes from `Cap`.
- `DefaultMaxValue` = `LoadoutPlanner.DefaultCap` (20,000 souls).
- `MaxValue`: the most a loadout's items may be worth now. Static, starts
  at `DefaultMaxValue`, so every upload or restart (a hot reload re-creates
  it) resets it to 20,000.

## `SetMaxValue(souls, mode)`

Sets `MaxValue` (callers validate with `LoadoutPlanner.TryParseCap`), logs
Information `Loadout cap set Previous= Cap=` in `loadout-*.log` and a master
line `Loadout cap <old> -> <new>`. Applies to the next `Apply`; loadouts
already on players are not touched.

## Operations

### `Apply(pawn, build, options, catalog = Default, mode, rng = Random.Shared)`

Gives a hero the power a real player of that build has at the same net
worth. Plans first, then follows the Deadworks Deathmatch example's
known-good order:

1. Plan the items: `cap = options.MaxValue ?? MaxValue`, `slots =
   options.Slots ?? ItemSlots.ForSouls(cap)` (9 below 16,000, 10, 11, then
   12 from 28,000: the slots a real hero has at that net worth), then
   `LoadoutPlanner.Plan(options.ItemOrder ?? ItemOrder(build, rng), catalog.ComponentsOf,
   catalog.CostOf, cap, slots, build.SellPriorityOf, ItemInfo.Exists,
   fillers, upgradesOf)`: buys in build order within the cap and the slots,
   and once the slots are full sells (by the build's sell priority, else
   cheapest and earliest) to make room for pricier items. Only when
   `ItemSlots.ExtraPasses(slots)` (all 12 open) are `OptionalItems(build)`
   and `catalog.UpgradesOf` passed, so empty slots are filled with the
   build's optional items (most expensive first) and held component items
   upgraded while the cap allows; below 12 nothing is backfilled or
   upgraded after the build order. Unknown items are collected. `value` is
   the held items' soul cost, never above `cap`.
2. Plan the power: `Progression.ForSouls(cap)` gives the level, boons,
   unlocks and ability points at the cap, so two builds at the same cap get
   the same level whatever their items cost; `AbilityPrefix(build.Abilities,
   unlocks, points)` gives the ranks the build's order has bought by then.
3. `pawn.ResetHero()`: wipes items and abilities.
4. `pawn.Level = progression.Level`, then `ModifyCurrency(EGold, 0, ECheats,
   silent)` so health and damage recalculate for the level (the boons).
5. Abilities: for each prefix entry, `AbilityComponent.FindAbilityByName`,
   then `UpgradeBits = bits`. Abilities missing on the hero are counted and
   logged at Trace.
6. Items: `pawn.AddItem(name)` for each planned item. For imbuable items
   (`ItemInfo.CanBeImbued`), it imbues into the build's target ability
   (`build.Imbues`), else into the first signature slot that accepts it
   (`CanImbue`).
7. `SetCurrency(EGold, options.Gold)`, `SetCurrency(EAbilityPoints, 0)`,
   `SetCurrency(EAbilityUnlocks, 0)`: the ranks are the build's, and the
   points left over after the prefix stopped are not handed out. Then
   `Heal(GetMaxHealth())`.

Returns a `LoadoutResult`. Logs Information `Loadout shopping Sold= Skipped=
Filled= Upgraded= Value= Cap=` (item names) when anything was sold, skipped,
filled or upgraded, one Information
line per loadout (`Loadout applied`, with `PlayerRef`, `Value`, `Cap`,
`Slots` (the item limit used), baseline, `Sold` / `Skipped` counts, `Level`, `Boons`, `Unlocks`, `Points`,
`PointsLeft`, `Steps` / `StepsTotal`, `Ranks` as `ability:bits` and
`ItemNames`, the planned items in order), and a
Warning when any item failed or was unknown.

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
  game refuses (for example, every open slot full). Slots are universal;
  9 are open unless every flex slot is unlocked (12). Items 10-12 fail
  while flex slots are locked.
- Setting `Level` directly avoids the per-level UI events of
  `ModifyCurrency` level-ups (see the Deathmatch example comment). It also
  grants no ability points, and `UpgradeBits` deducts none, so the wallets
  are set explicitly.
- Partial masks (`0b11`, `0b111`) are unverified in game. Budgeted
  loadouts use them; the `Ranks=` field of `Loadout applied` is what to
  check against the hero's ability panel.
