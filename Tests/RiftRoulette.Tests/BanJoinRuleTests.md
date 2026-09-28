# BanJoinRuleTests

Tests `RiftRoulette/Lobby/BanJoinRule` (linked source).

- A clean record gets a statue visit.
- Strikes lock out for 10 min, then 30 min, then until restart.
- A join inside a lockout is refused; at its end it is a statue visit again.
- After 3 strikes every join is refused.
- `Describe` text for no lockout, a running lockout, and a permanent one.
