# AccessList

In-memory form of `bublock/access.json`: open/private mode plus the banned
and allowed Steam64 ID sets. Pure; unit tested
(`Tests/RiftRoulette.Tests/AccessListTests`).

## File shape

```json
{
  "private": false,
  "banned": [],
  "allowed": []
}
```

## Operations

| Op | Behavior |
|---|---|
| `Private` | true = only allowed IDs and admins may join |
| `Banned` / `Allowed` | Sorted Steam64 ID sets |
| `Check(steamId, isAdmin)` | `AccessRule.Check` against the sets and mode |
| `ToJson()` | Indented JSON (camelCase keys, sorted IDs, trailing newline) |
| `Parse(json)` | Reads the file; throws `JsonException` on bad JSON or `null` |

## Parsing (hand edits)

- Keys are case-insensitive; missing `banned` / `allowed` mean empty.
- IDs may be numbers or quoted strings; `//` comments and trailing commas are allowed.
- IDs are not range-checked here. Commands validate IDs with
  `AccessRule.TryParseSteamId` before adding them.
