# HeroBans

Hero bans bought with betting souls: one hero out of the next round's
draw, for both teams. Pure (no game calls); one instance owned by
`RandomModeService`. Unit tested (`Tests/RiftRoulette.Tests/HeroBansTests`).

A ban costs `Cost` (1,000) souls, spent by the caller (`BetBook.TrySpend`)
after `TryBan` returns `Banned`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `TryBan(team, steamId, hero)` | One pending ban per team. A team that already has one gets `TeamAlreadyBanned` with that hero and the teammate who bought it; otherwise the ban is stored as pending (`Banned`). The other team's pending ban is not looked at, so both teams may ban the same hero | `HeroBanOutcome` |
| `TryGetPending(team, out hero, out steamId)` | The team's pending ban | bool |
| `Pending()` | Team number to pending hero | map |
| `PendingCount` | Pending bans | int |
| `Take(round)` | Moves every pending ban into `Current` (the draw's bans) and clears pending. Runs once per `round`: a second call for the same round (a reroll) returns the same set | set of heroes |
| `Current` | Bans in force for the round being prepared or fought | set of heroes |
| `Reset()` | Clears pending, current and the round cache | — |
| `RevealLine(names)` | `Banned this round: Haze, Lash` | string |

## Invariants

- At most one pending ban per team; a ban lasts exactly one draw.
- A ban bought after the draw waits for the next `Take`.
- Nothing is refunded: pending bans end with the match (`Reset`).
