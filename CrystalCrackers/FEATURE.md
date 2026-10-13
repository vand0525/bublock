# CrystalCrackers — Feature

## Purpose

A second game mode DLL (`CrystalCrackers.dll`): random heroes and stored
builds like Rift Roulette, but each round is fought once in the mid-boss
pit for the boss and its crystals. Mode rules and open questions:
`RiftRoulette/reference/crystal-crackers.md`.

## Current state

In development, no gameplay yet. The server runs it in place of Rift
Roulette during development (`scripts/deploy.sh`), always private.

- `CrystalCrackersPlugin` logs that the module host started.
- `AccessPlugin` + `AccessGate`: only admins and the `access.json` allowed
  list may join, whatever the file's mode; banned players are refused.
  Players without access are kicked 2 s after a load.

It compiles in the same reusable pieces Rift Roulette uses, as source:
`Shared`, and `Modules/WorldText`, `Movement`, `Hud`, `Loadout`,
`Restraint` (each with its commands), `Queue`, `Spectate`, `RandomHeroes`
and `Access`. Restrained players already take no damage here
(`RestraintPlugin.OnTakeDamage`).

## Files

| File | Role |
|---|---|
| `CrystalCrackersPlugin.cs` | Module host: load, startup, unload master lines |
| `AccessGate.cs` | Always-private verdict, connect gate, kick without access |
| `AccessPlugin.cs` | Connect hook, load kick pass, `/access_mode`, `/ban_*`, `/allow_*` |

## Public operations

`AccessGate.Check`, `AllowConnect`, `KickWithoutAccess`.

## State

None of its own; `AccessStore` caches `access.json` per load.

## Composition

- Lifecycle: `OnLoad`, `OnStartupServer`, `OnUnload` master lines;
  `OnClientConnect` gate; load kick pass.
- Admin commands: access lists (`AccessPlugin`), plus the imported module
  commands (`/wt_*`, `/mv_*`, `/hud_*`, `/loadout_*`, `/restrain*`,
  `/status_*`).

## Not here yet

Everything Rift Roulette's Lobby did for the server: team size, the 13th
admin seat, flex slots, pause guard, auto restart, buying off, soul rule,
hero enforcement. Added as the mode needs them. CleanSlate still clears
the map.

## Deploy

`scripts/deploy.sh` uploads `CrystalCrackers.dll` (with DevTools and
CleanSlate) and parks `RiftRoulette.dll` (backed up, removed from
`plugins/`, logs kept). Only one mode DLL may be on the server at a time:
both register the module and access commands.
