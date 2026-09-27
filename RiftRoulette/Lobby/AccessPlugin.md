# AccessPlugin

Thin plugin class (Name `Rift Roulette Access`) for the admin join-access
commands. Ops live on `AccessService`. The connect gate itself stays in
`LobbyPlugin.OnClientConnect` so one hook decides every connection.

## Hooks

| Hook | Does |
|---|---|
| `OnLoad` | `AccessService.Load()` (logs the lists once per load) |

## Commands (admin, Debug mode)

| Command | Does |
|---|---|
| `/player_ban <slot>` | Bans the connected player in `slot` (`AccessService.Ban`), then `LobbyService.KickPlayer`. Refuses an empty slot, a bot, or the caller |
| `/ban_add <steamid>` | `AccessService.Ban`, then `KickDenied` (a banned player who is connected is kicked) |
| `/ban_remove <steamid>` | `AccessService.Unban` |
| `/ban_list` | `AccessService.DescribeBanned` |
| `/allow_add <steamid>` | `AccessService.Allow` (whitelist for private mode) |
| `/allow_remove <steamid>` | `AccessService.Disallow` (does not kick) |
| `/allow_list` | `AccessService.DescribeAllowed` |
| `/access_mode` | `AccessService.Describe`: mode, file path, both lists |
| `/access_mode open` | `AccessService.SetPrivate(false)`; nobody is kicked |
| `/access_mode private` | `AccessService.SetPrivate(true)`, then `KickDenied` (connected players neither whitelisted nor admin are kicked) |

Every command calls `AdminCommand.Authorize` first (server console
trusted), replies with `AdminCommand.Reply` prefixed `[Access]`, and throws
`CommandException` for bad input. Steam IDs go through
`AccessRule.TryParseSteamId`, so a slot number typed by mistake is refused.

## Naming

Admin names carry a feature word and `_` so they stay out of the player
`/commands` list: `/player_ban` sits next to `/player_kick`, and
`/access_mode` is the open / private switch.
