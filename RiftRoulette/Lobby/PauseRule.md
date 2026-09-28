# PauseRule

Pure rules for turning pausing off. Unit tested
(`Tests/RiftRoulette.Tests/PauseRuleTests`). Used by `PauseGuard`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Commands` | Client console commands that pause: `pause`, `setpause`, `citadel_pause`, `citadel_toggle_server_pause` | list |
| `IsPauseCommand(command)` | Trimmed, case-insensitive match against `Commands`; null is false | bool |
| `ConVars(allowed)` | `citadel_allow_pausing` and `citadel_allow_pause_in_match` = 1 / 0 from `allowed`; `citadel_pause_allow_in_pregame` = 0 always (the game default) | name / value pairs |
| `ShouldUnpause(paused, allowed, nowMs, lastAttemptMs)` | True when the game is paused, pausing is off, and no unpause was tried in the last `UnpauseRetryMs` (2 s) | bool |
| `ShouldTell(nowMs, lastToldMs)` | True when the player was not told "pausing is off" in the last `TellCooldownMs` (5 s) | bool |

## Invariants

- Never sets `citadel_pause_count` or `citadel_num_team_pauses_allowed`:
  0 there means unlimited pauses.
- Times are wall-clock milliseconds (`Environment.TickCount64`), not game
  time, which stops while the game is paused.
