# TimedSession

One continuous session of fixed-length matches: no rounds, just
`Waiting` → `Playing` (the match clock) → `Break` (results) → `Playing`
again while enough players stay. The game type owns one instance (a
static), counts its players, gives the points, and draws its own banners
from the callbacks.

## Constructor

`TimedSession(name, options, players)`: `players` returns the current
player count; the game type decides who counts (humans, no spectators).

## State

`Phase`, `Match` (number, +1 at each start), `Scores` (`Scoreboard`,
cleared at each start and on stop), `Options`, the match end time
(`SecondsLeft`), and three timer handles (warning, end, next match). A hot
reload starts from `Waiting`.

## Operations

| Op | Behavior |
|---|---|
| `Check(timer, mode, players = null)` | `SessionRule.Decide(Phase, players ?? count, MinPlayers)`: `Start` or `Stop` as decided; Debug log; returns the action. On a disconnect pass the count without the leaver |
| `Start(timer, mode)` | Cancels timers, `Playing`, `Match + 1`, scores cleared, end time set; schedules `Warned` at `WarningSeconds` left and `End` at `MatchSeconds`; logs (feature and master); invokes `Started` |
| `End(timer, mode)` | While `Playing` or `Paused`: cancels timers, `MatchResult` (standings, `SessionRule.Winners`), `Break`, logs, invokes `Ended`; after `BreakSeconds` goes to `Waiting` and runs `Check`, so the next match starts while enough players remain. Returns the result, or null when not playing |
| `Pause(mode)` | Only while `Playing`: keeps the seconds left, cancels timers, `Paused` (no points: `IsPlaying` is false), logs, invokes `PausedAt` | paused |
| `Resume(timer, mode)` | Only while `Paused`: `Playing` again with the kept seconds (at least 1), warning and end rescheduled, logs, invokes `Resumed` | resumed |
| `AutoStart` | When false (a dev sandbox) a waiting session never starts by itself; `Start` / `Resume` still work. Passed to `SessionRule.Decide` | — |
| `Stop(mode)` | From `Playing` / `Break` / `Paused`: cancels timers, `Waiting`, scores cleared, logs `Session stopped`, invokes `Stopped` |
| `TrySetMatchSeconds(seconds)` | 30..1800; applies from the next match |
| `Describe(nameOf)` | Status line (phase, match, time left, length, min players), then one line per scorer |

## Callbacks

`Started(mode)`, `Warned(secondsLeft, mode)`, `Ended(result, mode)`,
`Stopped(mode)`, `PausedAt(secondsLeft, mode)`, `Resumed(secondsLeft, mode)`: all on the game type's timer, outside game events.

## Deadworks constraints

- Timers belong to the plugin whose `Timer` was passed and are dropped on
  hot reload (the whole DLL reloads, so the session restarts in `Waiting`).

## Logs

`session-YYYYMMDD.log` in the consuming DLL's folder; match start and end
also go to the master log.
