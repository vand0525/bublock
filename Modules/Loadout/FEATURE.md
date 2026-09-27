# Loadout — Feature

## Purpose

Reusable Deadlock hero loadouts built from real build data: the top 3 builds
per hero (by matches played, from the Deadlock API), applied to a pawn as the
first 9 items in build order plus the build's ability order. Game-agnostic
(no Rift Roulette dependency). Added in Stage 13b.

Stage 13c rules: every required item plus one random item per optional
group; Monster Rounds, Cultist Sacrifice and Golden Goose Egg are never bought (the next item
takes the slot); a loadout's items may be worth at most 20,000 souls (the
most expensive items are dropped, keeping at least 6). The median planned
value of all builds is the informational baseline.

## Public operations

| Op | Doc |
|---|---|
| `HeroBuildCatalog.Default` / `Heroes` / `BuildsFor` / `DisplayName` / `ComponentsOf` / `CostOf` / `PlannedValue` / `BaselineValue` / `TryParseHero` | `HeroBuildCatalog.md` |
| `HeroBuildData.Parse` | `HeroBuildData.md` |
| `LoadoutPlanner.ItemOrder` / `FirstSlots` / `Value` / `CapValue` / `AbilityBits` / `BitsFor` | `LoadoutPlanner.md` |
| `LoadoutService.Apply` / `Swap` / `Capture` / `ApplySnapshot` / `SwapSnapshot` | `LoadoutService.md` |
| `LoadoutSnapshot`, `SnapshotAbility`, `SnapshotItem`, `SnapshotResult` | `LoadoutSnapshot.md` |

## State

The lazily loaded `HeroBuildCatalog.Default` (read-only, one per DLL load).

## Data

- `Data/hero-builds.json`: written offline by `scripts/fetch-builds.py`,
  committed, and embedded in the DLL. It is never fetched at runtime.
- Refresh: run the script, then build and deploy.

## Units

| File | Role |
|---|---|
| `HeroBuildData.cs` | JSON records and parser (pure) |
| `HeroBuildCatalog.cs` | Embedded data and lookups |
| `LoadoutPlanner.cs` | Item slot and ability bit planning (pure) |
| `LoadoutSnapshot.cs` | Exact hero-state records for copying (Stage 13g) |
| `LoadoutService.cs` | Apply a build to a pawn; swap hero, then apply; capture / apply / swap a snapshot |
| `LoadoutPlugin.cs` | `/loadout_give`, `/loadout_copy`, `/loadout_list`, `/loadout_info` |
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
  `HeroBuildCatalogTests`).
