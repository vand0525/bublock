# MapRefreshRule

Pure decision for the join budget map refresh (`MapRefreshService`). No
Deadworks calls; linked into the test project.

A fighter-round is one fighter in one Random mode round, added up since
the map started.

## Constants

| Name | Value | Meaning |
|---|---|---|
| `DefaultBudget` | 160 | Fighter-rounds before the map reloads |

At 160 the refresh comes at the end of round 40 at 2v2, 27 at 3v3, 20 at
4v4, 16 at 5v5 and 14 at 6v6 (a bench player does not count).

## Operations

| Op | Returns |
|---|---|
| `Due(fighterRounds, budget)` | `true` when `budget > 0` and `fighterRounds >= budget` |
| `RoundsLeft(fighterRounds, budget, fighters)` | Rounds left at that team size (fighters in a round, both teams), rounded up; `0` once due; `null` when `budget <= 0` or `fighters <= 0` |

## Why

Every hero swap adds to the server's string tables (mostly
`AnimAssetData`), which are sent whole to every joining client. Past
512 KB the join package is refused and new players can't connect
(`Message size N is too big. Max is 524288`). Only a map reload clears the
tables. On 2026-09-28 the first failed join came after 284 fighter-rounds
(572 KB); growth was about 1.15 KB per fighter-round, so 512 KB was
crossed near 230. 160 leaves a margin. Never raise it without new
join-size data.
