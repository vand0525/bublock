# HeroBansTests

Unit tests for `RiftRoulette/RandomMode/HeroBans`.

- One pending ban per team; a teammate trying again gets `TeamAlreadyBanned`
  with the team's hero and buyer.
- Both teams may ban the same hero; the draw bans it once.
- `Take` moves pending bans into `Current` once per round number: a reroll
  returns the same set, a ban bought after the draw waits for the next
  round, and a round with no pending bans has none.
- `Pending` / `TryGetPending` show each team's ban.
- `Reset` clears pending, current and the round cache.
- `RevealLine` lists the heroes only.
