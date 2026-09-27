# StatsBoardText

Pure text for the side boards: one team's K / D / A (Random mode, Stage
13c) or the 1v1 best-streak leaderboard. Unit tested.

## Operations

| Op | Returns |
|---|---|
| `TeamBoard(teamName, rounds, rows)` | Line 1 `SAPPHIRE - 2 rounds` (`1 round` when one), line 2 `K / D / A   <team total>`, a blank line, then one `Name   K / D / A` line per player sorted by kills (desc), deaths (asc), then name; `No players` when empty |
| `RankStreaks(rows)` | Rows with `Best > 0`, sorted by best (desc), then name (ignore case) |
| `StreakLines(rows)` | `1  Name   5` per ranked row, or `No streaks yet` |
| `StreakBoard(rows)` | `STREAKS`, a blank line, then `StreakLines` |
| `Trim(name)` | Name cut to `NameLength` (16) characters |

`StatsRow(Name, Stats)` is one K / D / A row. `StreakRow(Name, Best)` is
one leaderboard row.
