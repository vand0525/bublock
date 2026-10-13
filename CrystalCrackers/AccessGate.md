# AccessGate

Crystal Crackers' join access: always private. Static. Reads
`bublock/access.json` through `Modules/Access/AccessStore` and checks with
`forcePrivate: true`, so only admins (`AdminAuth`) and the allowed list
get in, whatever the file's `private` value says. The file is never
written by the gate, so a later switch back to Rift Roulette finds its
mode unchanged.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Check(steamId)` | `AccessList.Check(steamId, AdminAuth.IsAuthorized(steamId), forcePrivate: true)`: `Banned` (a ban wins over everything), `Private` (not allowed, not admin) or `Allowed` | `AccessVerdict` |
| `AllowConnect(steamId, name)` | Steam ID 0 (bots) passes. Anything but `Allowed` is refused with an `access` Warning `Connection refused Verdict= Name= SteamId=` (also in master) | bool |
| `KickWithoutAccess(mode)` | Every connected human whose verdict is not `Allowed` is kicked (`kickid <slot>`), one Warning per kick | kicked names |

## Invariants

- No statues and no rejoin strikes (those are Rift Roulette's): a banned
  player is simply refused or kicked.
- Never kicks inside `OnClientConnect`; refusing there is the return value.
  Kicks come from a timer (`AccessPlugin.OnLoad`) or an admin command.
- `LoadKickSeconds` = 2: the delay after a load before the kick pass.
