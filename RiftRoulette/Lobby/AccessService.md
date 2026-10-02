# AccessService

Join access for bans and private mode. Static. The source of truth is
`access.json` in the `bublock/` folder on the server (the parent of
`LogPaths.ResolveRoot()`, i.e. `game/bin/win64/bublock/access.json`;
`Z:\gameserver\server\game\bin\win64\bublock\access.json` on this server).
See `AccessList.md` for the file shape.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `FilePath` | Absolute path of `access.json` | path |
| `Load()` | Missing file: open mode with empty lists. Otherwise re-reads the file only when its UTC write time changed; logs Information on each load. Unreadable or bad JSON: Error log, keeps the last good lists | `AccessList` |
| `AllowConnect(steamId, name)` | From `LobbyPlugin.OnClientConnect`. Steam ID 0 (bots) passes. Otherwise `AccessList.Check` with `AdminAuth.IsAuthorized`. `Banned`: `BanStatueService.AdmitBanned` decides (statue visit or refused inside a lockout; it logs). `Private`: refused with a Warning `server is private` | bool |
| `Check(player)` | Verdict for a connected player | `AccessVerdict` |
| `Ban` / `Unban` / `Allow` / `Disallow(steamId, mode)` | Changes one list, saves, Info + master line; no save when nothing changed | reply |
| `SetPrivate(privateMode, mode)` | Sets the mode, saves, Info + master line | reply |
| `SetStatueModifier(name, mode)` | Sets `statueModifier` (trimmed; blank or null clears it), saves, Info + master line | reply |
| `PetrifyBanned(timer, mode)` | Every connected human on the ban list who is not already a statue: `BanStatueService.Petrify(player, LiveBanKickSeconds (30), liveBan: true)` | names |
| `KickDenied(mode)` | Kicks every connected human whose verdict is `Private` (`LobbyService.KickPlayer`), Info per kick. Banned players are left to `PetrifyBanned` | kicked names |
| `Describe()` | Mode, statue modifier and path, then both lists on one line each | 3 lines |
| `DescribeBanned()` / `DescribeAllowed()` | Count, then one ID per line (with the name when connected). Banned lines add `BanStatueService.Describe` (rejoin strikes, lockout, `statue now`) | lines |

## Side effects

- `Save` writes `access.json.tmp`, then moves it over `access.json`
  (creates the `bublock/` folder if needed). A failed write logs an Error,
  forces a re-read on the next `Load`, and throws `CommandException` so the
  admin sees it.
- Kicks go through `LobbyService.KickPlayer`.

## Invariants

- The ban check runs before the seat rule and before admin status.
- A banned player is never kicked on the spot by a ban: they become a
  statue first (`BanStatueService`).
- Hand edits apply on the next connection or command without a rebuild or
  reload.
- The file never grants admin; admins are the compiled `AdminAuth` set.
- Deploys only upload DLLs, so `access.json` survives them.
