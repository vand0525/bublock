# DevTools — Feature

## Purpose

Server-side discovery and diagnostics for Deadlock/Deadworks experimentation
(entity find, inspect, remove, snapshot/diff, hero watch, log path).

## Current state

- Single plugin class `DevToolsPlugin` under `Bublock/DevTools/`, derived
  from the archive snapshot.
- Compiles in `Bublock/Shared/` via `Shared.projitems`.
- Builds with `Bublock.sln` / `scripts/update.sh`; ships as `DevTools.dll`
  (replaces the archive DLL of the same name on the server).

## Public operations

All admin-only (Debug-style diagnostics; no lifecycle caller):
`/dev_logpath`, `/ent_find`, `/ent_info`, `/ent_remove`, `/dev_herowatch`,
`/ent_snapshot`, `/ent_diff`. Details in `DevToolsPlugin.md` and
`RiftRoulette/reference/admin-commands.md`.

Every command is gated with `AdminCommand.Authorize` from `Bublock/Shared/`
(null caller = server console, trusted). Results reply to the caller's
console. Diagnostics log to `bublock/logs/DevTools/` (`master`, `commands`,
`herowatch`).

## State

Hero watcher flag/timer and the last entity snapshot, held on the plugin
instance (lost on reload).

## Relation to lifecycle

None. Admin commands only. Keep diagnostics here rather than stuffing
temporary probes into Rift Roulette.
