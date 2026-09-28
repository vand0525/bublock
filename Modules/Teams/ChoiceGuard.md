# ChoiceGuard

Which client console commands a game type refuses when it decides heroes
or teams itself. Pure; unit tested in `Tests/Modules.Tests`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Blocks(command, heroLocked, teamLocked)` | `selecthero` when `heroLocked`; `changeteam` / `jointeam` when `teamLocked`; trimmed, any case | bool |

## Use

A game type's plugin returns `HookResult.Stop` from `OnClientConCommand`
when `Blocks` is true. Server-side `SelectHero` / `ChangeTeam` calls are
not console commands and are never affected.

## Deadworks constraints

- The Deadworks Deathmatch example blocks exactly these three commands the
  same way (`reference/resources.md`, 2026-09-28). Rift Roulette instead
  kills a player who changes hero (`Lobby/HeroLock`); a game type picks one.
