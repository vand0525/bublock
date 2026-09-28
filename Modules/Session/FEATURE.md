# Session module

## Purpose

The match structure for continuous game types: one session that never
ends while players are on, cut into fixed-length matches (default 2
minutes) with a short results break between them, and a per-player
scoreboard. No rounds, no map objective. Added in the redock fork with Gun
Game; Rift Roulette keeps its own round-based loop.

## Files

| File | Role |
|---|---|
| `Scoreboard.cs` | Points per player, standings, places (pure, tested) |
| `SessionRule.cs` | Phases, options, start / stop decision, winners, clock text (pure, tested) |
| `TimedSession.cs` | The timer-driven loop with callbacks |
| `Session.projitems` | Compiles all three into a consumer |

## Public operations

See `Scoreboard.md`, `SessionRule.md`, `TimedSession.md`.

## State

Each `TimedSession` instance holds its phase, match number, scoreboard and
timers; the game type owns the instance.

## Commands and lifecycle

No plugin class, no commands. The game type:

1. creates `new TimedSession(name, options, countPlayers)` and sets the
   `Started` / `Warned` / `Ended` / `Stopped` callbacks (banners, rerolls);
2. calls `Check` after load, a little after each join, and on each leave;
3. adds points to `Scores` while `IsPlaying`;
4. wraps `Start`, `End`, `TrySetMatchSeconds` and `Describe` in its own
   admin commands.

## Dependencies

`Shared` (logging), `DeadworksManaged.Api` (timers).
