# AutoStartRuleTests

Unit tests for `GameLoop/AutoStartRule`.

- Disabled returns `None`, whatever the player count.
- One player alone: `None`. Two players, no match: `Start`.
- Running with 2 players: `None`. Running with 1 or 0 players: `End`.
- A disconnect (`leaving`) with 3 players left and no match: `None`; a
  disconnect below the minimum while running: `End`.
- A custom minimum (4) is respected.
