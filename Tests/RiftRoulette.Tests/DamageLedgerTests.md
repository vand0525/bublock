# DamageLedgerTests

Unit tests for `Stats/DamageLedger`.

- Exactly 20% of the victim's max health earns an assist; just below does
  not.
- Hits from one attacker add up within a life.
- The killer is never an assister; assisters come most damage first.
- Self-damage, zero Steam IDs and amounts of zero or less are ignored.
- An unknown max health (0) gives no assists.
- `Clear` drops only that victim; `Forget` drops a player as victim and
  attacker; `Reset` clears everything.
- A custom fraction is respected.
