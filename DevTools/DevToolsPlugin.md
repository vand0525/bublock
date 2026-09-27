# DevToolsPlugin

Deadworks admin/diagnostics plugin: entity find/inspect/remove, snapshot and
diff, hero watch, log path.

## Status

Derived from `archive/DevTools/DevToolsPlugin.cs` (Stage 2 snapshot).
- Stage 3: auth source moved to `Bublock/Shared/` (`AdminAuth`); unused
  `RunWithCheats` removed (shared copy is `Cheats.Run`).
- Stage 4: startup and auth diagnostics go to shared file logging; added
  `logpath`.
- Stage 12: SourceMod-style names (archive names removed, no aliases),
  every command admin-gated, results go to the caller's console.

## Commands

All commands call `AdminCommand.Authorize(caller, CommandsLog, "<name>")`.
A null caller (server console) is trusted; a non-admin player gets
"You are not allowed to use this command." and a `Warning` in the
`commands` log.

| Command | Old name | Caller | Behavior |
|---------|----------|--------|----------|
| `dev_logpath` | `logpath` (Stage 4) | player or console | Log root, DevTools log folder, session id, file-logging state |
| `ent_find <filter>` | `entities` | player or console | Entities whose designer/class/name contains the filter (case-insensitive): index, designer, class, name |
| `ent_info <designerName>` | `entity_info` | player or console | Field dump of every entity with that designer name |
| `ent_remove <designerName>` | `entity_remove` | player or console | Removes every entity with that designer name; one reply and one `commands` log line per entity, then a count |
| `dev_herowatch` | `herowatch` | player only | Toggles a 0.5 s timer watching the caller's hero id |
| `ent_snapshot` | `snapshot` | player or console | Remembers every entity (index, designer, class, name) on the plugin instance |
| `ent_diff` | `compare` | player or console | Entities added / removed since `ent_snapshot` (compares the full tuple, not the index alone) |

Command results go through `AdminCommand.Reply`: the caller's console, or the
server console for a null caller. The archive printed `entity_info`,
`entity_remove`, `snapshot`, `compare`, and `entities` to the server console
with `Console.WriteLine` (some with the old `[Rift Wars]` prefix); every line
now uses `[DevTools]`.

## Hero watcher

- Enabling cancels any previous timer, stores the caller's `EntityIndex`, and
  starts `Timer.Every(0.5.Seconds())`.
- Each tick finds the player by `EntityIndex`, reads `GetHeroPawn().HeroID`,
  and on change prints `[DevTools] HERO CHANGE | ...` to that player's console
  and logs `Hero change` (with `PlayerRef`) to the `herowatch` log.
- A second `dev_herowatch` (from any admin) disables it. `OnUnload` cancels
  the timer.

## Logging

Folder: `bublock/logs/DevTools/`.

| Event | File | Prefix |
|-------|------|--------|
| Loaded (`Reload=`), initialized | `master` | `[DevTools]` |
| Admin command accepted / rejected, each `ent_remove` removal | `commands` | `[DevTools.Commands]` |
| Hero watcher enabled / disabled / hero change | `herowatch` | `[DevTools.HeroWatch]` |

## Dangerous constraints

- `ent_remove info_super_trooper_spawn` removes a spawn entity the map relies
  on (crash risk). The command does not block it; admins must not run it.
- `Entities.All` is a property; `EntityIndex` is not globally unique, so the
  snapshot compares the full tuple.
