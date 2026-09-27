# Hud — Feature

## Purpose

Reusable, game-agnostic on-screen announcements: the game's HUD banner
(big title, smaller description) instead of chat. Compiled into consuming
DLLs as source; no Rift Roulette dependency. Added in Stage 13a.

## Public operations

| Op | Doc |
|---|---|
| `HudService.Announce(player, title, description, mode)` | `HudService.md` |
| `HudService.AnnounceAll(title, description, mode)` | `HudService.md` |
| `HudService.ParseAnnouncement(text)` | `HudService.md` |

## State

None.

## Units

| File | Role |
|---|---|
| `HudService.cs` | Banner ops |
| `HudPlugin.cs` | `/hud_announce` and `/hud_say` admin commands (thin wrappers) |
| `Hud.projitems` | Service (no commands) |
| `HudCommands.projitems` | Plugin class |

## Lifecycle vs commands

- Game code calls `AnnounceAll` in Clean mode (Rift Roulette:
  `GameLoop/MatchService` round start, round result with score, countdown,
  match end).
- Admin `/hud_announce` calls the same op in Debug mode, for trying banner
  text by hand. Admin `/hud_say <message>` uses it to talk to the server
  (message as the title, `Server admin` below), e.g. from the console while
  spectating. See `RiftRoulette/reference/admin-commands.md`.

## Consumers

- `RiftRoulette.dll` imports both projitems.
- `Tests/Modules.Tests` imports `Hud.projitems` for `ParseAnnouncement`.
