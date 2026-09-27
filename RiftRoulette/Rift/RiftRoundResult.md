# RiftRoundResult

Pure types describing how a rift round ended. Handed to the caller's
`RiftRoundSteps.RoundEnded` step once the round is fully over (phase back
to `Idle`). Added in Stage 13a so the match loop can score rounds.

## Types

- `RiftOutcome`: the outcome strings also shown as `LastOutcome` in
  `/rift_status`: `finished`, `tied`, `cancelled`, `spawn timed out`.
- `RiftRoundResult(Outcome, Side, WinnerTeam)`:
  - `Outcome`: one of `RiftOutcome`.
  - `Side`: the side of the round, or null if unknown.
  - `WinnerTeam`: team number of the first new rift trooper when the
    outcome is `finished` (the team that captured); null otherwise.

## Invariants

- No Deadworks calls; linked into `Tests/RiftRoulette.Tests`.
- `WinnerTeam` is read from the trooper entity's `TeamNum`. That the
  capturing team owns the spawned troopers is an assumption to confirm in
  game (the rift log line `Rift finished ... TrooperTeam=` shows it).
