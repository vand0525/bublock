# HeroBuildCatalogTests

Unit tests for `Modules/Loadout/HeroBuildData` and `HeroBuildCatalog`.

- A small inline JSON sample parses into builds, items, imbues, sell
  priorities (`SellPriorityOf`, 0 for unmarked items), ability steps, and
  the components map (and its reverse, `UpgradesOf`). Without
  `sellPriority` a build parses with none.
- `PlannedValue` follows the budget (1,000 gives 800, 5,000 gives 4,000,
  the default 20,000 buys all three items).
- `Plan` fills an empty slot from the optional group (the priciest item not
  already taken) and then upgrades a component item (`t1` to `t2`).
- `Heroes` skips IDs missing from the Deadworks `Heroes` enum and heroes
  with no builds.
- `TryParseHero` accepts enum names (case-insensitive) and game display
  names; `DisplayName` falls back to the enum name.
- `Default` loads the committed `Data/hero-builds.json` (embedded into the
  test assembly by `Loadout.projitems`): at least 20 heroes, 1–3 builds
  each, every build has items. `"Infernus"` maps to `Heroes.Inferno`.
- `Banned` includes Cultist Sacrifice; `Default` has no banned items (flat
  list or categories), every build has
  categories, some have optional groups, and every item has a cost.
- `Default.BaselineValue` sits between the cheapest and priciest planned
  build value.
- `BaselineValue` is the median planned value (inline sample of 800 / 3200 /
  6400); `CostOf` returns 0 for unknown items.
