# RestraintService

Keeps chosen players silenced and unable to use items, shoot or melee until
they are released (Stage 13h). They can still reload. Game agnostic: the caller decides who is restrained
(Rift Roulette restrains everyone sent up top, see `Round/WatchSpot`).

## State

- `Restrained`: set of Steam IDs. Survives death, respawn, `ResetHero` and
  hero swaps, because it is keyed by player, not pawn.
- `_tick`: frame counter for the modifier re-check.

## Operations

| Operation | Effect |
|---|---|
| `Restrain(player, mode)` | Adds the player. If the pawn is alive, applies the states and modifiers at once. False if already restrained. Logs Debug. |
| `Release(player, mode)` | Removes the player, removes the silence modifier from the pawn and turns the four states off. False if not restrained. Logs Debug with how many modifiers were removed. |
| `Forget(steamId)` | Drops the Steam ID without touching a pawn (disconnect). |
| `Sustain()` | Called every simulating frame by `RestraintPlugin.OnGameFrame`. Sets the states on every live restrained pawn; every `ModifierCheckTicks` (32) frames re-adds any missing modifier. No-op when nobody is restrained. |
| `AddModifier(pawn, name, seconds)` | `pawn.AddModifier(name, kv)` with `duration` set in a `KeyValues3`; true if the game returned a modifier. Also used by `/status_add`. |
| `IsRestrained(steamId)` / `Count` / `Describe()` | Queries; `Describe` lists each restrained player and which restraint modifiers are active. |

## What it applies

- Real game modifier (shows the game's own silence icon):
  `modifier_citadel_silenced`, with a `ModifierDurationSeconds` (100 000 s)
  duration so it doesn't expire.
- Modifier states, set every frame: `EModifierState.Silenced`,
  `ItemsDisabled` (14; blocks item actives, which silence alone does not),
  `ShootingDisabled` (62), `MeleeDisabled`. Items, shooting and melee have
  no modifier of their own here, so the states are the only blocks.
- No disarm: `modifier_citadel_disarmed` / `EModifierState.Disarmed` also
  block reloading, so players started rounds on an empty magazine
  (playtest 2026-09-27). `ShootingDisabled` blocks the gun and leaves
  reloading alone.

## Invariants

- A dead pawn is skipped; states and modifiers come back on the first live
  frame after respawn (states at once, modifiers within 32 frames).
- `Release` is the only way the effects are removed. Callers must release
  before moving a player into play.
- A refused modifier (unknown name, pawn not ready) is logged at Trace and
  retried on the next check, never thrown.

## Dangerous Deadworks constraints

- Modifier states are not sticky: the docs say many must be set every
  tick, hence `Sustain` from `OnGameFrame`. Keep `Sustain` cheap (it runs
  every frame).
- `KeyValues3` is `IDisposable`; always `using`.
- Whether `MeleeDisabled` blocks melee is still to be confirmed in game;
  fallback modifier to try with `/status_add`: `boss_victim_no_melee`.
- Playtest (2026-09-27): item actives still worked with `Silenced` alone,
  hence `ItemsDisabled`. If items still work, the fallback is the game's
  curse modifier (test by name with `/status_add` first).
- Whether `ShootingDisabled` alone (no modifier) blocks the gun is still to
  be confirmed in game; the enum also has `ReloadDisabled` (112) and
  `ManualReloadDisabled` (114), which must stay off.
