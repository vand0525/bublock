# DevTools — Feature

## Purpose

Server-side discovery and diagnostics for Deadlock/Deadworks experimentation
(entity find, inspect, remove, snapshot/diff, hero watch, log path).

## Current state

- Single plugin class `DevToolsPlugin` under `Bublock/DevTools/`, plus the
  `ModifierProbe` service.
- Uses `Bublock/Shared/`.
- Builds with `Bublock.sln` / `scripts/update.sh`; ships as `DevTools.dll`.

## Public operations

All admin-only (Debug-style diagnostics; no lifecycle caller):
`/dev_logpath`, `/ent_find`, `/ent_info`, `/ent_remove`, `/dev_herowatch`,
`/ent_snapshot`, `/ent_diff`. Details in `DevToolsPlugin.md` and
`RiftRoulette/reference/admin-commands.md`.

Every command is gated with `AdminCommand.Authorize` from `Bublock/Shared/`
(null caller = server console, trusted). Results reply to the caller's
console. Diagnostics log to `bublock/logs/DevTools/` (`master`, `commands`,
`herowatch`, `modifiers`).

Passive probe: `ModifierProbe` (via `DevToolsPlugin.OnAddModifier`) logs
each modifier name the first time the game adds it, to find names no data
file lists (Vyper's Petrify for the banned-player statue).

## State

Hero watcher flag/timer and the last entity snapshot, held on the plugin
instance (lost on reload); the modifier names seen (`ModifierProbe`, static,
reset on load).

## Relation to lifecycle

None. Admin commands only. Keep diagnostics here rather than stuffing
temporary probes into Rift Roulette.
