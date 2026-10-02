# AboutText

The chat lines `/about` sends: what Rift Roulette is (Random mode) and how
betting works. Pure; no Deadworks calls. Tested in
`Tests/RiftRoulette.Tests/AboutTextTests`.

## Operations

| Operation | Input | Output |
|---|---|---|
| `MirrorLines()` | — | three lines for mirror mode: everyone plays the same hero with the same build, even teams and the player sitting out, `/commands` (no betting lines; betting is Random mode only) |
| `Lines(bettingCloseSeconds)` | seconds into a round that betting closes (`BettingService.LingerSeconds`, passed in so this stays testable) | seven lines, in order: the mode (random hero and top build every round), even teams and the player sitting out, betting souls (start, per kill, per assist), how to bet and when it closes, the own-team rule, `/reserve`, `/heroban` and `/mark` with their prices (`/mark <slot>`, `MarkBook.Cost`: kill the marked enemy fighter that round to steal a quarter of their souls), `/souls` and `/commands` |

## Invariants

- Every number comes from the owning constant (`BetBook.StartingChips`,
  `ChipsPerKill`, `ChipsPerAssist`; `HeroReservations.Cost`, `Rounds`;
  `HeroBans.Cost`), so the text follows any change to them.
- Player text says souls, never chips.
