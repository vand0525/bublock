# DevPlugin (GunGame)

Dev / prod for Gun Game (`Modules/DevMode`), plus the dev-only environment
tools. All admin (`AdminAuth`; server console trusted). `/play`, `/stop`,
`/pause` are unprefixed by the owner's choice.

## Commands

| Command | Calls | Behavior |
|---|---|---|
| `/play` | `GunGameService.Play` | Paused: resume. Dev: save everyone's position, prod, auto-start on, `Live session` banner, start a match. Already live: says so |
| `/stop` | `GunGameService.Stop` | Dev, auto-start off, stop the session (no result), `Dev mode` banner, everyone back to their saved dev position on the next tick |
| `/pause` | `GunGameService.Pause` | Pause a live match (`Paused` banner, clock frozen, kills don't count) and take a debug snapshot (reply + `debug-*.log`); with nothing running, just the snapshot |
| `/gg_bots <0-11>` | `GunGameService.SetBots` | Dev only. `citadel_spawn_practice_bots` on with that count (bots then count as players), `0` turns it off and runs `bot_kick_all` with cheats |
| `/gg_map` | `GunGameService.ReloadMap` | Dev only. `changelevel <current map>`; players stay connected |
| `/gg_exec <command ...>` | `GunGameService.Exec` | Dev only. Runs a server console command; logged to the master log |
