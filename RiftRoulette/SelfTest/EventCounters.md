# EventCounters

Counts hook and event calls since the DLL loaded, so the self-test can tell
whether a hook still fires after a game update.

## Operations

- `Hit(name)`: adds one to `name`.
- `Count(name)`: calls since load (0 if never).

## Callers

| Name | Where |
|---|---|
| `client_connect`, `client_full_connect`, `client_disconnect`, `player_spawn`, `player_death` | `Lobby/LobbyPlugin` |
| `player_hero_changed` | `Draft/DraftPlugin` |
| `player_respawned` | `RandomMode/RandomPlugin` |
| `game_frame` | `GameLoop/GameLoopPlugin.OnGameFrame` |
| `modify_currency`, `soul_blocked_<Source>`, `ability_blocked_<Currency>_<Source>` | `GameLoop/GameLoopPlugin.OnModifyCurrency` (every call; each blocked gold or ability-point / unlock gain by `ECurrencySource`, not in `GameDependencies.Events`) |
| `pause_blocked_command`, `pause_blocked_message`, `pause_auto_unpause` | `Lobby/PauseGuard` (each blocked pause request by path, each automatic unpause attempt; not in `GameDependencies.Events`) |
| `take_damage`, `damage_blocked_restrained` | `GameLoop/GameLoopPlugin.OnTakeDamage` (every hit; each hit blocked because the victim is restrained up top, not in `GameDependencies.Events`) |

## Invariants

- Static state, reset by every load / hot reload (new load context).
- Game-thread only; no locking.
- Names checked by the self-test must match `GameDependencies.Events`;
  extra diagnostic names (`soul_blocked_*`, `ability_blocked_*`, `damage_blocked_restrained`,
  `pause_*`)
  are only counted.
