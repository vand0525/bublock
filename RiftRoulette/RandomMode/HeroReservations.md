# HeroReservations

Hero reservations bought with betting souls, as a waiting line per hero.
Pure (no game calls); one instance owned by `RandomModeService`. Unit
tested (`Tests/RiftRoulette.Tests/HeroReservationsTests`).

A reservation costs `Cost` (1,000) souls and lasts `Rounds` (3) rounds in
which the player fights with that hero. The souls are spent by the caller
(`BetBook.TrySpend`) before `TryReserve`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `TryReserve(steamId, hero)` | A player with a reservation (holding or waiting) gets `AlreadyHasOne` with that reservation. Otherwise joins the back of the hero's line: `Holding` when the line was empty, else `Waiting` with `Ahead` (players in front), `RoundsAhead` (their rounds left, added up) and `Holder` (front of the line) | `ReserveOutcome` |
| `Take(fighters, round)` | For each hero, the first player in its line who is in `fighters` plays it and uses one round (`ReservedTurn.Use` = 1..3); an entry at 0 leaves the line. Runs once per `round`: a second call for the same round (a reroll) returns the same turns, filtered to `fighters`, without counting again | turns |
| `TakeLate(steamId, round, playedThisRound)` | For a player who starts fighting after the round's draw (intermission joiner, bench player subbing in): null before `Take` ran for `round`, or when their hero is in `playedThisRound`; otherwise uses one round of their reservation (once per round) | turn or null |
| `Position(steamId)` | Hero, `Place` (0 = front), `RoundsAhead`, own `RoundsLeft`, `Holder` | position or null |
| `Count` | Reservations in every line | int |
| `Reset()` | Clears every line and the round cache | — |
| `RoundCount(n)` | `1 round` / `n rounds` | string |
| `WaitingLine(holder, hero, ahead, roundsAhead)` | One ahead: `<holder> has reserved <hero>. When their 3 rounds are done, it will be your turn.` More: `2 players are ahead of you for <hero> (5 rounds). Then it will be your turn.` | string |
| `TurnLine(hero, use)` | Use 1: `Your reserved hero is up: <hero> (round 1 of 3).` Later: `Reserved hero: <hero> (round 2 of 3).` | string |

## Invariants

- At most one reservation per player; at most one player per hero per round.
- A round counts only for a player in `fighters` who gets the hero; a
  benched or disconnected holder keeps their rounds, so a line never stalls.
- Lines keep the order reservations were bought in.
- Nothing is refunded: an unused reservation ends with the match (`Reset`).
