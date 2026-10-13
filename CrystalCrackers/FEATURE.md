# CrystalCrackers — Feature

## Purpose

A second game mode DLL (`CrystalCrackers.dll`): random heroes and stored
builds like Rift Roulette, but each round is fought once in the mid-boss
pit for the boss and its crystals. Mode rules and open questions:
`RiftRoulette/reference/crystal-crackers.md`.

## Current state

Skeleton only. `CrystalCrackersPlugin` logs that the module host started;
there is no match, round, pit or boss code yet.

It compiles in the same reusable pieces Rift Roulette uses, as source:
`Shared`, and `Modules/WorldText`, `Movement`, `Hud`, `Loadout`,
`Restraint` (each with its commands), `Queue`, `Spectate` and
`RandomHeroes`. Restrained players already take no damage here
(`RestraintPlugin.OnTakeDamage`).

## Public operations

None yet.

## State

None.

## Composition

- Lifecycle: `OnLoad`, `OnStartupServer`, `OnUnload` write master lines.
- Commands: only the imported module commands.

## Deploy

Not deployed. `scripts/update.sh` builds it with the solution, and
`scripts/deploy.sh` does not upload it. Only one mode DLL may be on the
server at a time: Rift Roulette and Crystal Crackers both register the
module commands (`loadout_*`, `mv_*`, `restrain`, `hud_*`, ...), which
collide. CleanSlate runs with either: it removes the Walkers and other
lane NPCs for every mode and keeps the vanilla mid-boss scheduler off, so
this mode will spawn its own boss.
