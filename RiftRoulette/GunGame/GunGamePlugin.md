# GunGamePlugin

Thin host for Gun Game (`GunGameService`, Stage 13k). No hooks: kills
arrive through `Stats/StatsService.RecordDeath`, which already credits them.

## Commands

| Command | Who | Calls | Reply |
|---|---|---|---|
| `/ladder` | players | `GunGameService.DescribePlayer` | your kills / target and place, then the top 3; or that Gun Game is not running |
| `/gungame_status` | admin | `GunGameService.Describe` | `Active=`, config, target, winner, then one line per player with kills |
| `/gungame_target <1-50>` | admin | `GunGameService.SetTarget` (Debug) | refused while a match runs or out of range |
| `/gungame_reroll <slot>` | admin | `GunGameService.Reroll` (Debug) | the player's new hero and build; needs a running Random mode match and a Random assignment for that player |

Admin commands check `AdminCommand.Authorize` with the `GunGame` log; the
server console is trusted. Replies start with `[GunGame]`. `/ladder` has
no `_`, so `/commands` lists it.
