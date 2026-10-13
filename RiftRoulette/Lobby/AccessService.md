# AccessService

Rift Roulette's join access: bans and private mode, with the game's
answer to each (statues, kicks). Static. The file itself
(`bublock/access.json`: load, cache, edits, atomic save) is
`Modules/Access/AccessStore`; this class wraps it so callers
(`LobbyPlugin`, `AccessPlugin`, `BanStatueService`, `PauseGuard`,
`HeroAssist/HotelProxyService`) keep one entry point. See
`Modules/Access/AccessList.md` for the file shape.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `FilePath`, `Load()` | `AccessStore.FilePath` / `AccessStore.Load()` | path / `AccessList` |
| `AllowConnect(steamId, name)` | From `LobbyPlugin.OnClientConnect`. Steam ID 0 (bots) passes. Otherwise `AccessList.Check` with `AdminAuth.IsAuthorized` (the file's mode). `Banned`: `BanStatueService.AdmitBanned` decides (statue visit or refused inside a lockout; it logs). `Private`: refused with a Warning `server is private` | bool |
| `Check(player)` | Verdict for a connected player | `AccessVerdict` |
| `Ban` / `Unban` / `Allow` / `Disallow` / `SetPrivate` / `SetStatueModifier` | Pass through to `AccessStore` (save, Info + master line, reply) | reply |
| `PetrifyBanned(timer, mode)` | Every connected human on the ban list who is not already a statue: `BanStatueService.Petrify(player, LiveBanKickSeconds (30), liveBan: true)` | names |
| `KickDenied(mode)` | Kicks every connected human whose verdict is `Private` (`LobbyService.KickPlayer`), Info per kick. Banned players are left to `PetrifyBanned` | kicked names |
| `Describe()` | `AccessStore.Describe()` | 3 lines |
| `DescribeBanned()` / `DescribeAllowed()` | `AccessStore.DescribeIds`; banned lines add `BanStatueService.Describe` (rejoin strikes, lockout, `statue now`) | lines |

## Side effects

- File writes and their errors: see `AccessStore.md`.
- Kicks go through `LobbyService.KickPlayer`.

## Invariants

- The ban check runs before the seat rule and before admin status.
- A banned player is never kicked on the spot by a ban: they become a
  statue first (`BanStatueService`).
- Hand edits apply on the next connection or command without a rebuild or
  reload.
- The file never grants admin; admins are the compiled `AdminAuth` set.
- Deploys only upload DLLs, so `access.json` survives them.
