# BalanceTracker

Pure counters and the auto-balance trigger (Stage 13c). Everything counts
since the last swap (or match start). Unit tested.

## Constants

| Const | Value | Meaning |
|---|---|---|
| `LeadRounds` | 2 | Round lead that makes a stomp check |
| `StompKillDiff` | 8 | Leader needs at least this many more kills |
| `StompKillRatio` | 1.5 | ...and at least this multiple of the other team's kills |
| `StreakRounds` | 5 | Rounds in a row that trigger without a stomp |

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `RecordRound(pointTo)` | Scoring round: rounds + 1 for that team; same team as the streak → streak + 1, else the streak restarts at 1 for that team. `null` or other teams (ties, cancels, timeouts) change nothing | — |
| `RecordKill(team)` | Kill count + 1 for Sapphire or Amber | — |
| `Check()` | `Stomp(leader)` when the round lead is at least `LeadRounds` and the leader's kills beat the other team's by `StompKillDiff` and by `StompKillRatio`x; else `Streak(team)` when the streak is at least `StreakRounds`; else `null` | `BalanceVerdict?` |
| `Leader()` | More rounds, then more kills; Sapphire on a full tie (used by `/balance_now`) | team number |
| `Reset()` | Zeroes everything | — |
| `Describe()` | `Since last swap: Rounds ... | Kills ... | Streak=Amber x3` | line |

`BalanceVerdict(Team, Reason)` with `BalanceReason` `Stomp`, `Streak`, or
`Forced` (`/balance_now`).
