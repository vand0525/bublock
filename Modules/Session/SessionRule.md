# SessionRule

Pure decisions and types for `TimedSession`. Unit tested in
`Tests/Modules.Tests`.

## Types

- `SessionPhase`: `Waiting` (too few players, or between break and next
  match), `Playing`, `Break` (results showing), `Paused` (clock frozen).
- `SessionAction`: `None`, `Start`, `Stop`.
- `SessionOptions(MatchSeconds = 120, BreakSeconds = 5, MinPlayers = 2,
  WarningSeconds = 10)`; `MinMatchSeconds` 30, `MaxMatchSeconds` 1800.
- `MatchResult(Match, Standings, Winners)`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Decide(phase, players, minPlayers, autoStart = true)` | Waiting: `Start` at `minPlayers` or more, only with `autoStart`. Paused: `None`. Playing / Break: `Stop` below `minPlayers`. Else `None` | `SessionAction` |
| `IsValidMatchSeconds(seconds)` | 30..1800 | bool |
| `Winners(standings)` | Everyone tied on the top score; empty when nobody scored | Steam IDs |
| `Clock(seconds)` | `2:00`, `0:07`; negatives show `0:00` (invariant culture) | string |
