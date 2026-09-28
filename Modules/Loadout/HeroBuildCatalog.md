# HeroBuildCatalog

Lookup over `HeroBuildData`: builds per hero, the playable hero pool, display
names, and item components.

## Operations

| Op | Behavior |
|---|---|
| `Default` | Lazily loads the embedded resource `Bublock.Modules.Loadout.hero-builds.json` from the consuming assembly (once per DLL load). Throws if the resource is missing or invalid. |
| `new HeroBuildCatalog(data)` | Indexes heroes by `Heroes` enum; skips IDs missing from the enum and heroes with no builds |
| `Heroes` | Heroes with at least one build, sorted by ID (the random pool) |
| `SkippedHeroIds` | Data IDs the constructor skipped (not in the enum, or no builds), in data order. Checked by `dw_selftest_run` |
| `BuildsFor(hero)` | Up to 3 builds in rank order; empty if none |
| `DisplayName(hero)` | Game name from the data (for example `Infernus`), else the enum name |
| `ComponentsOf(item)` | Direct component items; empty if none |
| `CostOf(item)` | Souls from `ItemCosts`; 0 if unknown |
| `UpgradesOf(item)` | Items that list `item` as a direct component (reverse of `ComponentsOf`, built once in the constructor); empty if none |
| `Plan(build, budget = 20,000, slots = null)` | `LoadoutPlanner.Plan` over `ItemOrder(build)` with the first item of each optional group (deterministic), this catalog's components and costs, the build's sell priorities, and an item limit of `slots ?? ItemSlots.ForSouls(budget)`. Only when `ItemSlots.ExtraPasses` (12 slots) does it pass `OptionalItems(build)` as fillers and `UpgradesOf` for the upgrade pass, the same rule as `LoadoutService.Apply` |
| `PlannedValue(build, budget = 20,000, slots = null)` | `Plan(...).Value` |
| `BaselineValue` | Median `PlannedValue` at the default budget over every loaded build, computed once in the constructor. Informational: logged with each loadout and shown by `/loadout_info` |
| `TryParseHero(text, out hero)` | Enum name (case-insensitive, for example `inferno`), else a game display name from the data (`Infernus`) |

## Side effects

The one-time resource read. When `Default` loads and some heroes were
skipped, it logs one Warning on the `loadout` log
(`Build data heroes skipped ... Ids= Pool=`), which shows a game update that
removed or renumbered a hero. The constructor itself never logs (tests build
catalogs directly).

## Invariants

- Data comes only from the committed JSON; no network access at runtime.
- The resource name is fixed by `Loadout.projitems` (`LogicalName`).
