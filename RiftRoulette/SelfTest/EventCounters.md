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
| `modify_currency`, `soul_blocked_<Source>` | `GameLoop/GameLoopPlugin.OnModifyCurrency` (every call; each blocked gain by `ECurrencySource`, not in `GameDependencies.Events`) |

## Invariants

- Static state, reset by every load / hot reload (new load context).
- Game-thread only; no locking.
- Names checked by the self-test must match `GameDependencies.Events`;
  extra diagnostic names (`soul_blocked_*`) are only counted.
