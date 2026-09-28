# AccessList

In-memory form of `bublock/access.json`: open/private mode, the banned and
allowed Steam64 ID sets, and the statue modifier name. Pure; unit tested
(`Tests/RiftRoulette.Tests/AccessListTests`).

## File shape

```json
{
  "private": false,
  "banned": [],
  "allowed": [],
  "statueModifier": "modifier_name"
}
```

`statueModifier` is left out when unset.

## Operations

| Op | Behavior |
|---|---|
| `Private` | true = only allowed IDs and admins may join |
| `Banned` / `Allowed` | Sorted Steam64 ID sets |
| `StatueModifier` | Modifier `BanStatueService` adds to banned players (null = restraint only) |
| `Check(steamId, isAdmin)` | `AccessRule.Check` against the sets and mode |
| `ToJson()` | Indented JSON (camelCase keys, sorted IDs, trailing newline) |
| `Parse(json)` | Reads the file; throws `JsonException` on bad JSON or `null` |

## Parsing (hand edits)

- Keys are case-insensitive; missing `banned` / `allowed` mean empty; a
  missing or blank `statueModifier` means unset.
- IDs may be numbers or quoted strings; `//` comments and trailing commas are allowed.
- IDs are not range-checked here. Commands validate IDs with
  `AccessRule.TryParseSteamId` before adding them.
