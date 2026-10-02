# MirrorPlugin

Thin host for Mirror mode: spawn hooks and admin commands. All logic is in
`MirrorModeService`.

## Hooks

| Hook | Behavior |
|---|---|
| `player_respawned` / `player_spawn` | Mirror mode with pending fighters: next tick, `MirrorModeService.ApplyPending` for that player (dead at prepare time, late joiners, hero swap guard kills) |

## Commands

All admin (`AdminAuth`; a null caller is the trusted server console), Debug
mode, `[Mirror]` replies. They work in any hero mode; pins only take effect
in mirror mode.

| Command | Calls | Reply |
|---|---|---|
| `/mirror_hero [hero\|clear]` | `MirrorModeService.PinHero` | Empty: the pins. A hero: `Mirror hero pinned: Haze. Build: one of its 3 builds each round, the same for everyone (/mirror_build 1-3 to pin). Applied now (6 swapped, 0 pending).` `clear`: both pins dropped |
| `/mirror_build [1\|2\|3\|clear]` | `MirrorModeService.PinBuild` | Refused with no hero pinned. Empty: the hero's numbered builds. A slot: `Mirror build pinned: Build 2: <name>. ...` `clear`: only the build pin dropped |
| `/mirror_status` | `MirrorModeService.Describe` | Pins, current shared pair, pinned hero's builds, one line per player |
