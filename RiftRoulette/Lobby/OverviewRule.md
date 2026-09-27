# OverviewRule

Pure timing for the stream camera's top-down view. Tested by
`OverviewRuleTests`.

## Operations

| Operation | Result |
|---|---|
| `Duration` | 10 s. |
| `EndFrom(now)` | `now + Duration`. |
| `Showing(end, now)` | True while `now < end`; false for a null end. |
| `CanStart(roundLive, roundNumber, lastShownRound)` | True only in a live round (`roundLive`, round number above 0) whose number differs from the last round a top-down was shown: at most one per round. |

## Invariants

- The round number comes from `RiftService.RoundNumber`, which only goes
  up during a DLL load, so every round has its own number.
- Manual `spec_overview` does not use this check.
