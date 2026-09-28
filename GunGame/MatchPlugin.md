# MatchPlugin (GunGame)

Thin host for kills and match commands.

## Hooks

- `player_death`: `GunGameService.OnDeath`.

## Commands

| Command | Who | Calls | Reply |
|---|---|---|---|
| `/points` | players | `GunGameService.DescribeFor` | chat lines |
| `/gg_status` | admin | `GunGameService.Describe` | `[GunGame]` lines |
| `/gg_end` | admin | `TimedSession.End` (Debug) | the result count, or no match running |
| `/gg_time <seconds>` | admin | `TimedSession.TrySetMatchSeconds` | new length from the next match, or the valid range |
| `/gg_reroll <slot>` | admin, dev only | `GunGameService.Reroll` (Debug) | the new hero and build |

Full catalog entries: `GunGame/FEATURE.md` → Commands.
