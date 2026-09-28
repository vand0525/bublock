# Loadout — Feature

## Purpose

Reusable Deadlock hero loadouts built from real build data: the top 3 builds
per hero (by matches played, from the Deadlock API), applied to a pawn by
shopping the build's item order within a soul cap, plus the build's ability
order. Game-agnostic (no Rift Roulette dependency). Added in Stage 13b.

Item rules: every required item plus one random item per optional
group; Monster Rounds, Cultist Sacrifice, Golden Goose Egg, Trophy
Collector and Healing Rite are never bought (the next item takes the
slot). Budget planner (2026-09-28, `LoadoutPlanner.Plan`): items are bought
in order while the held items' value stays within the cap (20,000 souls by
default; unaffordable items are skipped, later cheaper ones still fit),
into 12 universal slots (Rift Roulette opens every flex slot). With the
slots full it sells the build's marked sell-priority items first, else the
cheapest and earliest, to make room for pricier items. After the build
list, empty slots are filled from the build's optional items, most
expensive first, and a final pass upgrades every held component item
(T1 to T2 to T3) while the cap allows. The median planned
value of all builds at 20,000 is the informational baseline.

The cap is adjustable at runtime (2026-09-28): `/loadout_cap <souls>`
(1,000 to 200,000, or `default`) sets `LoadoutService.MaxValue` for the
next builds handed out. It is in memory only and resets to 20,000 on every
upload or restart.

Budgeted power: the level, boons and ability ranks come from the cap, not
the items' value, so every build at the same cap has the same level. The
cap goes through `Progression.ForSouls` (Deadlock's level table), and the
build's ability order is applied only as far as that level's unlocks and
points pay for (`LoadoutPlanner.AbilityPrefix`). Both ability wallets are
left at 0. Copies (`ApplySnapshot`) stay exact.

## Public operations

| Op | Doc |
|---|---|
| `HeroBuildCatalog.Default` / `Heroes` / `BuildsFor` / `DisplayName` / `ComponentsOf` / `UpgradesOf` / `CostOf` / `Plan` / `PlannedValue` / `BaselineValue` / `TryParseHero` | `HeroBuildCatalog.md` |
| `HeroBuildData.Parse`, `HeroBuild.SellPriorityOf` | `HeroBuildData.md` |
| `LoadoutPlanner.ItemOrder` / `OptionalItems` / `Plan` / `SellCandidate` / `Value` / `TryParseCap` / `AbilityBits` / `AbilityPrefix` / `BitsFor` | `LoadoutPlanner.md` |
| `Progression.ForSouls` / `Max` / `MaxLevel` | `Progression.md` |
| `LoadoutService.Apply` / `Swap` / `Capture` / `ApplySnapshot` / `SwapSnapshot` / `MaxValue` / `SetMaxValue` | `LoadoutService.md` |
| `LoadoutSnapshot`, `SnapshotAbility`, `SnapshotItem`, `SnapshotResult` | `LoadoutSnapshot.md` |

## State

The lazily loaded `HeroBuildCatalog.Default` (read-only, one per DLL load)
and `LoadoutService.MaxValue` (the current cap, reset on every load).

## Data

- `Data/hero-builds.json`: written offline by `scripts/fetch-builds.py`,
  committed, and embedded in the DLL. It is never fetched at runtime.
- Refresh: run the script, then build and deploy.

## Units

| File | Role |
|---|---|
| `HeroBuildData.cs` | JSON records and parser (pure) |
| `HeroBuildCatalog.cs` | Embedded data and lookups |
| `LoadoutPlanner.cs` | Budgeted item shopping (buy, sell, skip, fill, upgrade) and ability bit planning, including the budgeted ability prefix (pure) |
| `Progression.cs` | Deadlock level table: boons, unlocks and ability points per soul count (pure) |
| `LoadoutSnapshot.cs` | Exact hero-state records for copying (Stage 13g) |
| `LoadoutService.cs` | Apply a build to a pawn; swap hero, then apply; capture / apply / swap a snapshot |
| `LoadoutPlugin.cs` | `/loadout_give`, `/loadout_show`, `/loadout_copy`, `/loadout_list`, `/loadout_info`, `/loadout_cap` |
| `Loadout.projitems` | Service and data (no commands) |
| `LoadoutCommands.projitems` | Plugin class |

## Lifecycle vs commands

- Rift Roulette `RandomMode/RandomModeService` calls `LoadoutService.Swap` in
  Clean mode for every player at each intermission in Random mode.
- `Swap` and `SwapSnapshot` re-issue `SelectHero` up to 3 tries (1 s
  apart) when the hero has not changed yet; a `SelectHero` in the same
  frame as another one can be lost (2026-09-27 playtest: no builds after a
  join-triggered auto-start).
- Rift Roulette `Duel/DuelService` calls `Capture` for `/duel_copy` and
  `SwapSnapshot` (0 gold) for both players each 1v1 intermission.
- Admin `/loadout_give` and `/loadout_copy` call the same ops in Debug mode
  (test tools). See
  `RiftRoulette/reference/admin-commands.md`.

## Consumers

- `RiftRoulette.dll` imports both projitems.
- `Tests/Modules.Tests` imports `Loadout.projitems` (`LoadoutPlannerTests`,
  `ProgressionTests`, `HeroBuildCatalogTests`, `LoadoutSnapshotTests`).
