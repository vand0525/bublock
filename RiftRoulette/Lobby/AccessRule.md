# AccessRule

Pure join-access rule for bans and private mode. Unit tested
(`Tests/RiftRoulette.Tests/AccessRuleTests`).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Check(banned, privateMode, allowed, isAdmin)` | `Banned` when banned (even an allowed admin); `Private` when private and the player is neither allowed nor admin; otherwise `Allowed` | `AccessVerdict` |
| `IsSteamId(steamId)` | True for an individual Steam64 ID (`SteamIdBase` < id ≤ base + 2³²−1) | bool |
| `TryParseSteamId(text, out steamId)` | Parses a trimmed number and checks `IsSteamId` | bool |

## Invariants

- A ban always wins. The ban list is checked before the whitelist and the admin set.
- Open mode (`privateMode` false) lets in anyone who is not banned.
- `IsSteamId` rejects slot numbers and short typos (for example `3`) so they
  never reach the lists.
