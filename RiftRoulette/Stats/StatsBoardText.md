# StatsBoardText

Pure text for the side boards: one team's K / D / A. Unit tested.

## Operations

| Op | Returns |
|---|---|
| `TeamBoard(teamName, rounds, rows)` | Line 1 `SAPPHIRE - 2 rounds` (`1 round` when one), line 2 `K / D / A   <team total>`, a blank line, then one `Name   K / D / A` line per player sorted by kills (desc), deaths (asc), then name; `No players` when empty |
| `Trim(name)` | Name cut to `NameLength` (16) characters |

`StatsRow(Name, Stats)` is one K / D / A row.
