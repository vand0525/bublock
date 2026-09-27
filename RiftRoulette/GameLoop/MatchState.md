# MatchState

Pure match bookkeeping for the continuous playtest match: phase, round
number, score, ties. No Deadworks calls; linked into
`Tests/RiftRoulette.Tests` (`MatchStateTests`). One instance lives on
`MatchService.State`.

## Types

- `MatchPhase`: `Idle` (no match), `Intermission` (countdown to the next
  round), `InRound` (a rift round is running).

## Members

| Member | Behavior |
|---|---|
| `Phase`, `Round`, `Sapphire`, `Amber`, `Ties` | Current phase, rounds started, points per team, tied rounds |
| `IsRunning` | `Phase != Idle` |
| `Start()` | Reset, then phase `Intermission` |
| `BeginRound()` | `Round + 1`, phase `InRound` |
| `EnterIntermission()` | Phase `Intermission` |
| `Apply(result)` | `tied` adds a tie. `finished` gives 1 point to `WinnerTeam` when it is Sapphire (3) or Amber (2) and returns that team; every other case returns null (no point) |
| `FormatScore()` | `Sapphire 2 - 1 Amber` |
| `DescribeResult(result, pointTo)` | Banner text: `Sapphire took the rift`, `Tied - no point`, `Round cancelled - no point`, `Rift did not spawn - no point`, or `Rift taken, winner unknown - no point` |
| `Reset()` | Phase `Idle`, all counters 0 |

## Invariants

- Only `finished` with a known team scores. Cancelled and timed-out rounds
  never score.
