# BetBoardText

Pure text for the betting leaderboard board. Unit tested
(`Tests/RiftRoulette.Tests/BetBoardTextTests`).

## Operations

| Operation | Result |
|---|---|
| `Rank(rows)` | `BetRow(Name, Chips)` sorted by chips (highest first), then name (case-insensitive); at most `MaxRows` (8) |
| `Board(rows)` | `BETTING`, a blank line, then `1  Name   1,200` per ranked row (names cut to `StatsBoardText.NameLength`), or `No chips yet` |
| `Format(chips)` | Thousands separator, invariant culture (`1,200`) |

## Invariants

- The caller passes the rows to show (connected participants); this file
  only sorts and formats.
