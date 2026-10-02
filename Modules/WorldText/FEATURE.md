# WorldText — Feature

## Purpose

Reusable, game-agnostic in-game text boards (`point_worldtext`): create,
update, remove, list, and clear by string id. No Rift Roulette dependency.

## Public operations

| Op | Doc |
|---|---|
| `WorldTextService.Create(id, spec, mode)` | `WorldTextService.md` |
| `WorldTextService.Update(id, text, mode)` | `WorldTextService.md` |
| `WorldTextService.Remove(id, mode)` | `WorldTextService.md` |
| `WorldTextService.ClearAll(mode)` | `WorldTextService.md` |
| `WorldTextService.List()` | `WorldTextService.md` |
| `WorldTextPlacement.InFrontOf` / `FacingViewer` | `WorldTextPlacement.md` |
| `WorldTextFormat.FromArgs` / `Preview` | `WorldTextFormat.md` |

Types: `WorldTextSpec`, `WorldTextColor`.

## State

`WorldTextService` owns the registry of boards it created (id -> entity +
spec). One registry per DLL (separate load contexts).

## Units

| File | Role |
|---|---|
| `WorldTextService.cs` | Core ops + registry |
| `WorldTextSpec.cs`, `WorldTextColor.cs` | Board description |
| `WorldTextPlacement.cs`, `WorldTextFormat.cs` | Pure helpers (unit tested) |
| `WorldTextPlugin.cs` | `/wt_*` admin commands (thin wrappers) |
| `WorldText.projitems` | Service + types (no commands) |
| `WorldTextCommands.projitems` | Plugin class |

## Lifecycle vs commands

- Game code (Rift Roulette `Boards/BoardService.Redraw`) calls the service in
  Clean mode: `ClearAll()` then `Create("board.*", ...)`.
- Admin commands (`/wt_list`, `/wt_create`, `/wt_update`, `/wt_remove`,
  `/wt_clear`) call the same ops in Debug mode. See
  `RiftRoulette/reference/admin-commands.md`.

## Consumers

- `RiftRoulette.dll` imports both projitems.
- `Tests/Modules.Tests` imports `WorldText.projitems` for the pure helpers.
