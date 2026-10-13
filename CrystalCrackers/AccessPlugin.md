# AccessPlugin

Thin plugin class (Name `Crystal Crackers Access`): the always-private
connect gate and the access list commands. Ops live on `AccessGate` and
`Modules/Access/AccessStore`.

## Hooks (Clean mode)

| Hook | Does |
|---|---|
| `OnLoad(isReload)` | `AccessStore.Load()`, master `Access gate on, always private Reload=`, then 2 s later (`AccessGate.LoadKickSeconds`) `AccessGate.KickWithoutAccess()`; master `Kicked players without access Count=` when any. Runs on the first upload (players left from Rift Roulette) and every hot reload |
| `OnClientConnect` | `AccessGate.AllowConnect(SteamId, Name)`: admins and the allowed list only; bans refused |

## Commands

| Command | Who | Does |
|---|---|---|
| `/access_mode` | admin | Says Crystal Crackers is always private, then `AccessStore.Describe()` (file mode, lists, path). Any argument is refused (`the mode can't be changed here`) |
| `/ban_add <steamid>` | admin | `AccessStore.Ban(Debug)`, then `KickWithoutAccess(Debug)`; reply lists anyone kicked |
| `/ban_remove <steamid>` | admin | `AccessStore.Unban(Debug)` |
| `/ban_list` | admin | `AccessStore.DescribeIds("Banned", ...)` |
| `/allow_add <steamid>` | admin | `AccessStore.Allow(Debug)` |
| `/allow_remove <steamid>` | admin | `AccessStore.Disallow(Debug)`, then `KickWithoutAccess(Debug)` |
| `/allow_list` | admin | `AccessStore.DescribeIds("Allowed", ...)` |

Every command calls `AdminCommand.Authorize` first (server console
trusted) and replies with `AdminCommand.Reply`. Steam IDs are checked with
`AccessRule.TryParseSteamId`; a bad one throws `CommandException`.

## Constraints

- Same command names as Rift Roulette's `Lobby/AccessPlugin`; fine only
  because the two mode DLLs are never on the server together.

## Logging

`access-YYYYMMDD.log` in `bublock/logs/CrystalCrackers/`; refusals and
kicks are Warnings, so they also reach master.
