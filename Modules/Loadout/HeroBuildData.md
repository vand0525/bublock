# HeroBuildData

Records for the build data file (`Data/hero-builds.json`, written by
`scripts/fetch-builds.py`) and its JSON parser. Pure; no Deadworks calls.

## Types

| Type | Fields |
|---|---|
| `HeroBuildData` | `FetchedAt`, `Source`, `WindowDays`, `Heroes`, `Components` (item to direct component items), `ItemCosts` (item to souls; optional) |
| `HeroBuildSet` | `Id` (Deadlock hero ID = `Heroes` enum value), `ClassName`, `Name` (game display name), `Builds` |
| `HeroBuild` | `BuildId`, `Version`, `Name`, `Rank` (`matches` / `weekly_favorites` / `favorites`), `Matches`, `Wins`, `Favorites`, `Items` (flat build order), `Imbues` (item to ability class name), `Abilities`, `Categories` (optional), `SellPriority` (optional: item to the build's non-zero sell priority; higher sells first) |
| `BuildCategory` | `Name`, `Optional` (an option group: one item is picked), `Items` |
| `AbilityStep` | `Ability` (class name), `Kind` (`unlock` / `upgrade`) |

## Operations

- `HeroBuildData.Parse(Stream)` / `Parse(string)`: `System.Text.Json` with
  web defaults (camelCase, case-insensitive). Throws `InvalidDataException`
  on an empty document and `JsonException` on bad data.
- `HeroBuild.SellPriorityOf(item)`: the item's sell priority, 0 when the
  build has none for it (or no `SellPriority` at all).

## Invariants

- Collections are nullable in the records; consumers treat null as empty.
- Integer fields must be numbers (the fetch script writes 0, never null).
