# PitRoundPlugin

Thin host for the pit round: hooks and the `/pit_*` admin commands.

## Hooks

| Hook | Does |
|---|---|
| `OnLoad` | `citadel_check_walk_on_skyclip 0` (`ServerConVars.TrySet`; the watch spots stand on the sky clip), `PitLocations.RegisterAll()`, subscribes `PitRoundService.OnBossKilled` / `OnCrystalTaken` to `MidBossService.BossKilled` / `CrystalTaken`, `WatchCredit` every 1 s |
| `OnUnload` | Unsubscribes both events |
| `OnStartupServer` | The sky clip convar again, `PitRoundService.ResetForMap()` |
| `OnClientDisconnect` | Not a map change: `PitRoundService.Forget` |
| `player_death` | `PitRoundService.OnDeath(controller, pawn)` |
| `player_spawn` | `PitRoundService.OnSpawn(controller, Timer)` |
| `OnModifyCurrency` | `SoulRule.ShouldBlock(..., PitRoundRule.BlocksSouls(Phase))`: earned souls and ability points blocked while a round runs (cheats, starting amount and item sales pass, so loadouts still set gold); blocked gains logged at Debug in `pit-*.log` |

## Commands (admin, `AdminCommand.Authorize`, Debug mode)

| Command | Calls |
|---|---|
| `/pit_start [amber\|sapphire]` | `PitRoundService.Start` (team in the pit; left out, it alternates) |
| `/pit_end` | `PitRoundService.End(Admin)` (no winner) |
| `/pit_status` | `PitRoundService.Describe` |
| `/pit_timing [window] [limit]` | `PitRoundService.SetTiming` (window 5-120 s, limit 30-1800 s; no arguments shows them) |

Bad arguments throw `CommandException` with the allowed range.
