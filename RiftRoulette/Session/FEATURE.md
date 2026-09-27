# Rift Roulette Session — Feature

## Purpose

Session lifecycle lines in the Rift Roulette master log (load, map startup,
unload), so every log file shares one session story. On load it also
registers the Rift Roulette locations with Movement
(`RiftRouletteLocations.RegisterAll()`).

## Public operations

- `/session_info` (admin): session id, round id, map, log folder,
  file-logging state. See `SessionPlugin.md` and
  `reference/admin-commands.md`.

## State

None. The session id and round id come from `BublockLog` (one per DLL load).

## Composition

`SessionPlugin` only. Later stages may add round start/end lines here or in
the Rift/GameLoop plugin classes.

## Lifecycle vs commands

Hooks are lifecycle (Clean mode). `/session_info` is a read-only admin
command.
