# HeroReservationsTests

Unit tests for `RiftRoulette/RandomMode/HeroReservations`.

- The first reservation on a hero holds it; a player can have only one
  reservation (a second one reports the first).
- Later reservations on the same hero wait, with their place and the
  rounds of everyone ahead of them.
- `Take` counts a round once per round number: a reroll in the same round
  returns the same turns; after 3 rounds the reservation is gone.
- A holder who is not fighting keeps their rounds, and the next fighter in
  line plays the hero that round.
- The holder finishing moves the next player to the front.
- Never two players on one hero in a round.
- `TakeLate` does nothing before the round's draw, refuses a hero someone
  plays this round, and counts a late turn once.
- `Reset` clears everything.
- `WaitingLine` names the holder (one ahead) or counts the line;
  `TurnLine` says which of the 3 rounds this is.
