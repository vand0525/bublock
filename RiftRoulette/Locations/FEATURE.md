# Rift Roulette Locations — Feature

## Purpose

Rift Roulette-specific map positions for the generic Movement module (draft
area, the watch spots above each rift, and rift start spawns), kept out of `Modules/Movement` so the module
stays game-agnostic.

## Public operations

| Member | Doc |
|---|---|
| `RiftRouletteLocations.Draft`, `WatchGreen`, `WatchYellow`, `WatchCenter`, `GreenSapphire`, `GreenAmber`, `YellowSapphire`, `YellowAmber`, `CenterSapphire`, `CenterAmber`, `All` | `RiftRouletteLocations.md` |
| `RiftRouletteLocations.RegisterAll()` | `RiftRouletteLocations.md` |

## Invariants

- Sapphire (team 3, base at +y) starts at +y on both lanes, Amber at -y.
- The watch spots' camera angles face the welcome board
  (`Round/WatchLayout`), checked by `WatchLayoutTests`.

## State

None of its own; `RegisterAll()` writes into `MovementService.Locations`.

## Composition

- Lobby, Round, and Rift pass the typed locations to `MovementService`.
- `SessionPlugin.OnLoad` calls `RegisterAll()` so admins can use
  `/mv_tp draft`, `/mv_tp green_sapphire`, etc.

## Lifecycle vs commands

Data only. No hooks, no commands.
