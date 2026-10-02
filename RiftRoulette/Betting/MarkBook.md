# MarkBook

Pure bookkeeping of marks (`/mark`): a fighter pays betting souls to mark
an enemy fighter for one round; killing them that round steals a share of
their souls. No game calls, so it is unit tested
(`Tests/RiftRoulette.Tests/MarkBookTests`). One instance lives in
`BettingService` (`Marks`).

A mark costs `Cost` (300) souls, spent by the caller (`BetBook.TrySpend`)
before `Place`. The steal is `BetBook.Steal(victim, killer, StealDivisor)`
(4: a quarter of the victim's total).

## State

- One `Mark(TargetId, TargetName, Round)` per marker Steam ID. The name is
  kept for messages after the target leaves; `Round` is the match round
  the mark is for.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Place(markerId, targetId, targetName, round)` | Refuses a zero ID, marking yourself, or a marker who already holds a mark; otherwise stores it. Several markers may mark the same target | bool |
| `TryGet(markerId, out mark)` | The marker's mark | bool |
| `TryConsume(markerId, victimId, round)` | True and removes the mark only when the marker holds a mark on `victimId` for `round` | bool |
| `TakeAll()` | Every mark in marker Steam ID order, then clears them (round end, match end) | list of `(Marker, Mark)` |
| `Count`, `Marks` | Held marks | int, map |
| `Reset()` | Clears every mark | — |

## Invariants

- At most one mark per marker; a mark pays off once.
- Consuming one marker's mark leaves other marks on the same target.
- No soul bookkeeping here: spending, refunds and steals are `BetBook`.
