# Rift Roulette — Admin Commands

Admin / debug / force commands. Keep this catalog accurate whenever
commands are added, renamed, restricted, or removed.

Populate entries from verified source as commands are extracted. Do not
invent undocumented commands. Do not invent Steam IDs; authorize from
existing DevTools / source only.

Admin gate: `AdminAuth.IsAuthorized` in `Bublock/Shared/Auth/AdminAuth.cs`
(the single shared Steam ID set, compiled into every Bublock DLL). New admin
commands call it through `AdminCommand.Authorize`
(`Bublock/Shared/Auth/AdminCommand.cs`).

## Entry template

```markdown
### /command_name <args>

- **Invocation:** chat `/command_name` | console `dw_…` (if any)
- **Who:** admin (Steam ID from verified source)
- **Calls:** core operation(s) …
- **Mode:** Debug ON (manual admin invocation)
- **Side effects:** …
- **Notes:** …
```

## Commands

Full behavior and ownership for every command: `behavior-inventory.md`.

### WorldText (`WorldTextPlugin`, in RiftRoulette.dll)

All five: admin (`AdminCommand.Authorize`; server console trusted; rejected
callers get "You are not allowed to use this command." and a Warning in the
`WorldText` log with name and Steam ID). Ops run in Debug mode, so per-board
lines appear in `worldtext-*.log`. Results go to the caller's console. Text
arguments take the rest of the line; type `\n` for a line break.

#### /wt_list

- **Invocation:** chat `/wt_list` | console `dw_wt_list`
- **Who:** admin
- **Calls:** `WorldTextService.List`
- **Mode:** Debug; read-only
- **Side effects:** prints the count, then `id | Position | preview` (40 characters) for each board created through WorldText (draft boards appear as `draft.welcome`, `draft.sapphire`, `draft.amber`)

#### /wt_create <id> <text...>

- **Invocation:** chat `/wt_create <id> <text>` | console `dw_wt_create <id> <text>`
- **Who:** admin, in game (needs a hero pawn; server console gets an error)
- **Calls:** `WorldTextPlacement.InFrontOf` / `FacingViewer`, `WorldTextService.Create`
- **Mode:** Debug
- **Side effects:** creates a white board (scale 0.8, font 64) 150 units in front of the caller's eyes, facing them; replaces any board with the same id

#### /wt_update <id> <text...>

- **Invocation:** chat `/wt_update <id> <text>` | console `dw_wt_update <id> <text>`
- **Who:** admin
- **Calls:** `WorldTextService.Update`
- **Mode:** Debug
- **Side effects:** changes the board's text in place; error if the id is unknown. Draft boards are redrawn by the game on the next pick/unpick/reset, which overwrites manual edits

#### /wt_remove <id>

- **Invocation:** chat `/wt_remove <id>` | console `dw_wt_remove <id>`
- **Who:** admin
- **Calls:** `WorldTextService.Remove`
- **Mode:** Debug
- **Side effects:** removes one board; error if the id is unknown

#### /wt_clear

- **Invocation:** chat `/wt_clear` | console `dw_wt_clear`
- **Who:** admin
- **Calls:** `WorldTextService.ClearAll`
- **Mode:** Debug
- **Side effects:** removes **every** `point_worldtext` on the map (including draft boards and any text not created through WorldText) and replies with the count. Draft boards come back on the next draft redraw

### Movement (`MovementPlugin`, in RiftRoulette.dll)

All eight: admin (`AdminCommand.Authorize`; server console trusted; rejected
callers get "You are not allowed to use this command." and a Warning in the
`Movement` log with name and Steam ID). Ops run in Debug mode, so
per-player teleport lines appear in `movement-*.log`. Results go to the
caller's console. `[slot]` defaults to the caller; from the server console
a slot is required. Location names are case-insensitive. Built-in
locations (registered by Rift Roulette on load): `draft`, `green_sapphire`,
`green_amber`, `yellow_sapphire`, `yellow_amber`.

A teleport keeps the model angle, zeroes velocity, and sets the camera to
the location's angle (same as the archive). Players with no hero pawn are
skipped; there is no alive check (the archive had none either).

#### /mv_list

- **Invocation:** chat `/mv_list` | console `dw_mv_list`
- **Who:** admin
- **Calls:** `MovementService.Locations.List`
- **Mode:** Debug; read-only
- **Side effects:** prints the count, then `name | Position | Angle` for each location sorted by name; saved ones are marked `saved`

#### /mv_where [slot]

- **Invocation:** chat `/mv_where [slot]` | console `dw_mv_where <slot>`
- **Who:** admin
- **Calls:** `MovementService.Where`
- **Mode:** Debug; read-only
- **Side effects:** prints the player's name, slot, position, and eye angles; error if the slot has no hero

#### /mv_tp <location> [slot]

- **Invocation:** chat `/mv_tp <location> [slot]` | console `dw_mv_tp <location> <slot>`
- **Who:** admin
- **Calls:** `MovementService.TeleportTo`
- **Mode:** Debug
- **Side effects:** teleports one player to the location and sets their camera; error if the location is unknown, the slot is empty, or the player has no hero
- **Notes:** `/mv_tp draft` replaces the archive `/test`, which anyone could run (old name removed in Stage 12)

#### /mv_tp_team <team number> <location>

- **Invocation:** chat `/mv_tp_team <team> <location>` | console `dw_mv_tp_team <team> <location>`
- **Who:** admin
- **Calls:** `MovementService.TeleportPlayers`
- **Mode:** Debug
- **Side effects:** teleports every player whose team number matches; replies `Moved N of M`. Takes a team number (module commands stay game-agnostic)

#### /mv_tp_all <location>

- **Invocation:** chat `/mv_tp_all <location>` | console `dw_mv_tp_all <location>`
- **Who:** admin
- **Calls:** `MovementService.TeleportPlayers`
- **Mode:** Debug
- **Side effects:** teleports every connected player with a hero; replies `Moved N of M`

#### /mv_angle <pitch> <yaw> <roll> [slot]

- **Invocation:** chat `/mv_angle <pitch> <yaw> <roll> [slot]` | console `dw_mv_angle <pitch> <yaw> <roll> <slot>`
- **Who:** admin
- **Calls:** `MovementService.SetViewAngle`
- **Mode:** Debug
- **Side effects:** sends a camera-angle message to that player only; no position change

#### /mv_save <name>

- **Invocation:** chat `/mv_save <name>` | console: not usable (needs a hero)
- **Who:** admin, in game
- **Calls:** `MovementService.Where`, `MovementService.Locations.Register(saved: true)`
- **Mode:** Debug
- **Side effects:** saves the caller's position and view (pitch, yaw, roll 0) under the name; replaces an earlier saved location with that name. Names use letters, digits, `_ . -`. Built-in names are refused. Saved locations live in memory only and are lost on plugin reload

#### /mv_remove <name>

- **Invocation:** chat `/mv_remove <name>` | console `dw_mv_remove <name>`
- **Who:** admin
- **Calls:** `MovementService.Locations.Unregister`
- **Mode:** Debug
- **Side effects:** removes a location saved with `/mv_save`; built-in locations cannot be removed

### Lobby (`LobbyPlugin`, in RiftRoulette.dll)

All: admin (`AdminCommand.Authorize`; server console trusted; rejected
callers get "You are not allowed to use this command." and a Warning in the
`Lobby` log with name and Steam ID). Ops run in Debug mode. Results go to
the caller's console. Player lines use `LobbyService.DescribePlayer`: slot,
name, Steam ID, team, pick, hero, life state, alive, health, max health,
position, entity index.

#### /player_list

- **Invocation:** chat `/player_list` | console `dw_player_list`
- **Who:** admin
- **Calls:** `LobbyService.ListPlayers`
- **Mode:** Debug; read-only
- **Side effects:** prints the count, then one line per fully connected player
- **Notes:** replaces the archive's unused `LogPlayerLifeStates`

#### /player_info <slot>

- **Invocation:** chat `/player_info <slot>` | console `dw_player_info <slot>`
- **Who:** admin
- **Calls:** `LobbyService.DescribePlayer`
- **Mode:** Debug; read-only
- **Side effects:** prints one player's line (also logged in `players-*.log`); error if the slot is empty

#### /player_kick <slot>

- **Invocation:** chat `/player_kick <slot>` | console `dw_player_kick <slot>`
- **Who:** admin
- **Calls:** `LobbyService.KickPlayer`
- **Mode:** Debug
- **Side effects:** logs the kick with the player's name and Steam ID; releases their pick and redraws the draft boards if they had one; runs `kickid <slot>`. Empty slot: Warning in `lobby` (copied to master) and an error reply
- **Notes:** replaces the archive `/kick`, which anyone could run (old name removed in Stage 12; now admin-only, intentional difference)

#### /player_team <slot> <sapphire|amber>

- **Invocation:** chat `/player_team <slot> <team>` | console `dw_player_team <slot> <team>`
- **Who:** admin
- **Calls:** `RiftRouletteTeams.TryParse`, `LobbyService.SetTeam`
- **Mode:** Debug
- **Side effects:** moves a player without a pick to Sapphire (3) or Amber (2) with `ChangeTeam`. Errors for an unknown team name, an empty slot, or a player who has a pick (the pick decides their team; they must unpick first)
- **Notes:** team names are allowed here because Lobby is Rift Roulette code, not a module

#### /lobby_setup

- **Invocation:** chat `/lobby_setup` | console `dw_lobby_setup`
- **Who:** admin
- **Calls:** `LobbyService.ApplyServerConvars`
- **Mode:** Debug
- **Side effects:** re-applies the startup convars and commands (team size 6, max players 13 with 12 shown in the browser (the 13th is the admin seat), KOTH off and warning times 1, override spawn time 1, purchasing anywhere, duplicate heroes, pause convars from `PauseGuard`). Note `citadel_koth_enabled 0` would close the KOTH gate if run mid-rift

#### /pause_allow [on|off]

- **Invocation:** chat `/pause_allow [on|off]` | console `dw_pause_allow [on|off]`
- **Who:** admin
- **Calls:** `PauseGuard.Describe` (no argument) or `PauseGuard.SetAllowed` (`on` / `off`, also `1` / `0`)
- **Mode:** Debug
- **Side effects:** no argument: one line with pausing on / off, `GamePaused`, `ServerPaused`, blocked requests and automatic unpauses since load. `on` / `off`: sets `citadel_allow_pausing` and `citadel_allow_pause_in_match` to 1 / 0 (`citadel_pause_allow_in_pregame` stays 0) and writes a master line `Pausing turned on/off`. While off, pause console commands (`pause`, `setpause`, `citadel_pause`, `citadel_toggle_server_pause`) and the pause net messages (`CCLCMsg_RequestPause`, `CCitadelClientMsg_Pause`) are blocked for everyone, admins included; the player gets the chat line `Pausing is off on this server.` (at most once per 5 s), and a game that is paused anyway gets the server `pause` toggle every 2 s until it runs again (`pause-*.log`)
- **Notes:** new 2026-09-27 (a player kept pausing the lobby). Off after every load and hot reload; `on` lasts until the next one. Error for any other argument

#### dw_seat_spec

- **Invocation:** console only (`ConsoleOnly`), no arguments: `dw_seat_spec` from the client dev console or the server console. Chat `/seat_spec` does not run it
- **Who:** admin. Targets the caller, or without a caller (server console, or a client command that arrives without one) the connected player with the admin Steam ID (`AdminAuth.SteamIds`); replies `The admin is not connected.` otherwise
- **Calls:** `AdminSeat.Sit` (→ `DraftState.Release`, `RandomModeService.Forget`, `DuelService.Forget`, `RestraintService.Release`, `WatchGuard.Forget`, `StatsService.RefreshBoards`, `AutoStartService.Check`; next tick `ChangeTeam(1, false)` + `MakeObserver()`)
- **Mode:** Debug
- **Side effects:** works at any time, also during a rift round. Moves the admin to the spectator side, outside Sapphire and Amber, and removes their hero pawn (observer camera). They stop counting for teams, auto-start, stats, Random mode and 1v1; a running match may auto-end if fewer than 2 players remain, and the teams are evened at the next Random mode intermission. Logs `Admin seat taken, spectating next tick Phase=...`, then `Admin spectating TeamNum=... HeroPawn=... Observer=...`, and a master line
- **Notes:** new in Stage 13f; console only since 2026-09-27. `ChangeTeam(1)` alone left the hero pawn alive and dropped the admin's client 12-23 s later (two crashes on 2026-09-27); `MakeObserver` fixes that, so the round refusal is gone. Spectators cannot type in game chat, so getting back out needs the console. Every admin is seated automatically on connect (use `dw_seat_play` to play)

#### /seat_play

- **Invocation:** console `dw_seat_play` (client or server console), no arguments
- **Who:** admin. Same target as `dw_seat_spec`: the caller, else the connected player with the admin Steam ID
- **Calls:** `AdminSeat.Stand` (→ `LobbyService.AdmitPlayer`)
- **Mode:** Debug
- **Side effects:** takes the admin out of the seat and admits them like a new connection: smaller team, Skyrunner, watch spot (restrained), Random mode joiner or 1v1 setup souls, auto-start check. 2 s later logs whether a hero spawned (`Admin hero after leaving the seat`, or the Warning `Admin has no hero after leaving the seat`)
- **Notes:** new in Stage 13f. Refused when not seated, or when 12 players are already playing. At 09:39 on 2026-09-27 a `dw_seat_play` from the seated admin arrived without a caller and stopped at "pass the slot" (the reply went to the server console). Both seat commands dropped the slot argument after that and fall back to the admin Steam ID

#### /seat_status

- **Invocation:** chat `/seat_status` | console `dw_seat_status`
- **Who:** admin
- **Calls:** `AdminSeat.Describe`
- **Mode:** Debug; read-only
- **Side effects:** playing count and cap (12), seated count, the `maxplayers` and `sv_visiblemaxplayers` values, then per seated admin: slot, name, team number, whether a pawn exists
- **Notes:** new in Stage 13f. Use it to check that `maxplayers 13` took effect

### Stream camera (`LobbyPlugin`, `Lobby/StreamCam`)

While an admin is seated as an observer the stream camera runs by itself
(no command needed): it follows a live player's view, cuts to the killer
when the watched player dies, goes top-down over the rift for 10 s on the
first big teamfight ult of each round (`Lobby/BigUlts`) and then to the
ult user's view (another live player if they died), and parks top-down
over the current rift when nobody plays. These commands are optional. They
take no player argument: they target the caller, else the connected admin
(same fallback as `dw_seat_spec`). Spectators have no chat, so use the
client console (`dw_spec_*`).

#### /spec_auto <on|off>

- **Invocation:** console `dw_spec_auto on` / `dw_spec_auto off` | chat `/spec_auto <on|off>`
- **Who:** admin
- **Calls:** `StreamCam.SetAuto`
- **Mode:** Debug
- **Side effects:** turns the automatic camera on or off for that admin (default on) and clears any top-down in progress. Off leaves the camera where it is for manual control. `on`/`1`, `off`/`0`; anything else errors. Resets to on when the admin stands up or disconnects
- **Notes:** new with the stream camera (2026-09-27)

#### /spec_status

- **Invocation:** console `dw_spec_status` | chat `/spec_status`
- **Who:** admin
- **Calls:** `StreamCam.Describe`
- **Mode:** Debug; read-only
- **Side effects:** three lines: auto, seated, observer, observer mode; who is on camera, parked and side; top-down showing or off, return-to player, round number, whether a round is running, whether this round's top-down is used
- **Notes:** new with the stream camera (2026-09-27)

#### /spec_overview

- **Invocation:** console `dw_spec_overview` | chat `/spec_overview`
- **Who:** admin
- **Calls:** `StreamCam.ShowOverview` (→ `SpectateService.Park` 264 units above `WatchSpot.Location(WatchSpot.Side)`, pitch 89)
- **Mode:** Debug
- **Side effects:** puts the admin's client in fly cam (`spec_mode 4`), then top-down over the rift now for 10 s, then back to the player the camera was on (or the next choice). Does not use up the round's automatic top-down. Errors when the admin is not spectating
- **Notes:** new with the stream camera (2026-09-27); also the quickest in-game check that teleporting the observer camera works

### Access (`AccessPlugin`, in RiftRoulette.dll)

Admin (`AdminCommand.Authorize` with the `Access` log; server console
trusted). Debug mode; `[Access]` replies. The lists and mode live in
`game/bin/win64/bublock/access.json` on the server (`{"private": false,
"banned": [], "allowed": []}`, Steam64 IDs). The file can be edited by hand;
the change applies on the next connection. `LobbyPlugin.OnClientConnect`
checks it before the admin seat rule. A banned ID (even an admin) is let
in as a statue and kicked 10 s later (`BanStatueService`); each such visit
is a strike, and the next reconnect is refused for 10 min, then 30 min,
then until restart. Private mode refuses anyone neither whitelisted nor an
`AdminAuth` admin. Refusals and statue visits are Warnings in
`access-*.log` (copied to master) with name and Steam ID. Steam ID
arguments must be real Steam64 IDs (17 digits, starting 7656119); slot
numbers are refused.

#### /player_ban <slot>

- **Invocation:** chat `/player_ban <slot>` | console `dw_player_ban <slot>`
- **Who:** admin
- **Calls:** `AccessService.Ban`, `AccessService.PetrifyBanned`
- **Mode:** Debug
- **Side effects:** adds the connected player's Steam ID to `banned` and saves the file. The player turns to stone up top (out of the game, restrained, statue modifier), is told `You are banned. Do better. You will be kicked in 30s.`, everyone else sees `<name> is banned.`, and they are kicked 30 s later. Refuses an empty slot, a bot, or yourself

#### /ban_add <steamid>

- **Invocation:** chat `/ban_add <steamid>` | console `dw_ban_add <steamid>`
- **Who:** admin
- **Calls:** `AccessService.Ban`, `AccessService.PetrifyBanned`
- **Mode:** Debug
- **Side effects:** adds the ID to `banned` and saves; if that player is connected they turn to stone and are kicked 30 s later (as `/player_ban`). Replies `... is banned.` or `... was already banned.`, plus `Turned to stone, kicked in 30s: <names>.`

#### /ban_remove <steamid>

- **Invocation:** chat `/ban_remove <steamid>` | console `dw_ban_remove <steamid>`
- **Who:** admin
- **Calls:** `AccessService.Unban`
- **Mode:** Debug
- **Side effects:** removes the ID from `banned` and saves

#### /ban_list

- **Invocation:** chat `/ban_list` | console `dw_ban_list`
- **Who:** admin
- **Calls:** `AccessService.DescribeBanned`
- **Mode:** Debug; read-only
- **Side effects:** count, then one ID per line (with the name if connected), each with its rejoin record: `Strikes=N`, `locked M min` or `locked until restart`, `statue now`

#### /ban_modifier [name|none]

- **Invocation:** chat `/ban_modifier [name|none]` | console `dw_ban_modifier [name|none]`
- **Who:** admin
- **Calls:** `AccessService.SetStatueModifier` (no argument: reads `AccessService.Load().StatueModifier`)
- **Mode:** Debug
- **Side effects:** no argument shows the modifier banned players get (`none (restraint only)` when unset). A name saves it as `statueModifier` in `access.json`; `none` clears it. Find the name in DevTools `modifiers-*.log` after someone casts Vyper's Petrify. A name the game refuses logs a Warning once per statue; the statue is then restrained only

#### /allow_add <steamid>

- **Invocation:** chat `/allow_add <steamid>` | console `dw_allow_add <steamid>`
- **Who:** admin
- **Calls:** `AccessService.Allow`
- **Mode:** Debug
- **Side effects:** adds the ID to the whitelist (`allowed`) and saves. Only matters in private mode

#### /allow_remove <steamid>

- **Invocation:** chat `/allow_remove <steamid>` | console `dw_allow_remove <steamid>`
- **Who:** admin
- **Calls:** `AccessService.Disallow`
- **Mode:** Debug
- **Side effects:** removes the ID from the whitelist and saves. Does not kick; a connected player stays until they reconnect

#### /allow_list

- **Invocation:** chat `/allow_list` | console `dw_allow_list`
- **Who:** admin
- **Calls:** `AccessService.DescribeAllowed`
- **Mode:** Debug; read-only
- **Side effects:** count, then one ID per line (with the name if connected)

#### /access_mode [open|private]

- **Invocation:** chat `/access_mode`, `/access_mode private`, `/access_mode open` | console `dw_access_mode [open|private]`
- **Who:** admin
- **Calls:** no argument: `AccessService.Describe`; `open` / `private`: `AccessService.SetPrivate`, and for `private` `AccessService.KickDenied`
- **Mode:** Debug
- **Side effects:** no argument prints the mode, the file path, and both lists. `private` saves the mode and kicks every connected player who is neither whitelisted nor an admin (reply lists who). `open` saves the mode and kicks nobody; anyone not banned can join again (the 12-player seat cap still applies)

### Draft (`DraftPlugin`, in RiftRoulette.dll)

All: admin (`AdminCommand.Authorize`; server console trusted; rejected
callers get "You are not allowed to use this command." and a Warning in the
`Draft` log with name and Steam ID). Ops run in Debug mode, so detail lines
appear in `draft-*.log`. Results go to the caller's console with a
`[Draft]` prefix. In Random mode (`/match_mode random`, the default since
Stage 13b) `/draft_assign` and `/draft_release` reply "Heroes are random
this match..." and change nothing, and `/draft_boards` draws the welcome
board plus the Sapphire / Amber stats boards (Stage 13c) instead of the
pool boards. 1v1 mode (Stage 13g) behaves the same way (draft off, stats
boards), and Draft's hero enforcement never runs there. In Random mode Draft's hero enforcement defers to
`RandomModeService.GuardHero`: a player who changes hero from the menu
after their build was applied is killed and respawns with their assigned
hero and build (the death is not counted in stats).

#### /draft_status

- **Invocation:** chat `/draft_status` | console `dw_draft_status`
- **Who:** admin
- **Calls:** `DraftService.DescribeDraft`
- **Mode:** Debug; read-only
- **Side effects:** prints both pools (taken heroes marked), the pick count, then `Hero | Name | slot N | SteamID=` per pick (`(offline)` when the picker is not connected)

#### /draft_assign <slot> <hero>

- **Invocation:** chat `/draft_assign <slot> <hero>` | console `dw_draft_assign <slot> <hero>`
- **Who:** admin
- **Calls:** `DraftService.Pick` for the player in that slot
- **Mode:** Debug
- **Side effects:** same rules and effects as that player running `/pick <hero>` (refusals included). The outcome line goes to the player's chat and to the caller's console. Error if the slot is empty

#### /draft_release <slot>

- **Invocation:** chat `/draft_release <slot>` | console `dw_draft_release <slot>`
- **Who:** admin
- **Calls:** `DraftService.Unpick` for the player in that slot
- **Mode:** Debug
- **Side effects:** same rules and effects as that player running `/unpick`. The outcome line goes to the player's chat and to the caller's console. Error if the slot is empty

#### /draft_reset

- **Invocation:** chat `/draft_reset` | console `dw_draft_reset`
- **Who:** admin
- **Calls:** `DraftService.Reset`
- **Mode:** Debug
- **Side effects:** clears all picks; for every alive player zeroes gold, ability points, and level, moves them to team 2 as Skyrunner (Random mode keeps their team so teams stay even), and teleports them to the draft area next tick; dead players are skipped (Debug line in `draft-*.log`); redraws boards; replies with the number of players reset
- **Notes:** replaces the archive `/reset`, which anyone could run (old name removed in Stage 12; now admin-only, intentional difference)

#### /draft_boards

- **Invocation:** chat `/draft_boards` | console `dw_draft_boards`
- **Who:** admin
- **Calls:** `DraftService.RedrawBoards`
- **Mode:** Debug
- **Side effects:** removes **every** `point_worldtext` on the map, then redraws the three draft boards from the current picks (Random mode: welcome board plus the two stats boards)
- **Notes:** new in Stage 9; use after `/wt_clear` or manual `/wt_update` edits

### Rift (`RiftPlugin`, in RiftRoulette.dll)

All: admin (`AdminCommand.Authorize`; server console trusted; rejected
callers get "You are not allowed to use this command." and a Warning in the
`Rift` log with name and Steam ID). Ops run in Debug mode, so every step
(give-up time, cash-in VData, dead players skipped, per-player teleports)
appears in `rift-*.log`; round start / spawn / end / cancel also go to the
master log. Results go to the caller's console with a `[Rift]` prefix. Only
one rift runs at a time.

#### /rift_start

- **Invocation:** chat `/rift_start` | console `dw_rift_start`
- **Who:** admin
- **Calls:** `RoundFlow.RunRound` (the same composed path the lifecycle runs in Clean mode) → `RiftService.RunRift` (`RiftGameRules.ConfigureNextRift`, wait for spawner, `ParkScheduler`, `RoundFlow.MoveTeamsToRift`, `AlternateSide`, watch, `EndRound` with `RoundFlow.ReturnPlayersToDraft`)
- **Mode:** Debug
- **Side effects:** forces the next rift (green / yellow alternating, starts green) through the game's KOTH scheduler. When the spawner appears: parks the natural scheduler (KOTH off), moves Sapphire picks and Amber picks to their own slot spots around that side's team starts (`SlotSpots.Fight`), flips the next side. Watches each tick: new troopers mean finished; a cash-in that appears then disappears means tied. 3 s later returns alive players, restrained, to the watch spot above the next rift, and moves the boards there (dead ones return through Lobby's `player_spawn`) and removes troopers spawned since the start. If no spawner appears within 320 ticks: parks the scheduler, Warning in the log, side not flipped. Sets the log round id `r<n>` for the rift
- **Notes:** replaces the archive `/koth`, which anyone could run (old name removed in Stage 12; now admin-only, intentional difference). Refuses with "A rift is already running (Phase=...). Use /rift_cancel." while a rift is in progress. In the archive a second `/koth` could not spawn a second rift and only timed out after 320 ticks; the refusal reports that immediately (intentional difference). Replies "Could not reach CCitadelGameRules" (Error in log) if gamerules cannot be found. Known-good sequence; do not alter

#### /rift_status

- **Invocation:** chat `/rift_status` | console `dw_rift_status`
- **Who:** admin
- **Calls:** `RiftService.DescribeRift`
- **Mode:** Debug; read-only
- **Side effects:** prints phase (Idle / WaitingForSpawn / Live / Ending), current side, next side, last outcome (none / finished / tied / spawn timed out / cancelled), round id, and the trooper snapshot size

#### /rift_next <green|yellow>

- **Invocation:** chat `/rift_next <side>` | console `dw_rift_next <side>`
- **Who:** admin
- **Calls:** `RiftSides.TryParse`, `RiftService.SetNextSide`, `WatchSpot.MoveAllUp`
- **Mode:** Debug
- **Side effects:** sets the side of the next `/rift_start`, then moves every live player (restrained) and the boards to the watch spot above that side. Replies `Next rift: <side>. N player(s) moved to the watch spot.` Errors for an unknown side or while a rift is running (the flip on spawn would overwrite it)
- **Notes:** new in Stage 10; the middle rift is not selectable

#### /rift_cancel

- **Invocation:** chat `/rift_cancel` | console `dw_rift_cancel`
- **Who:** admin
- **Calls:** `RoundFlow.CancelRound` → `RiftService.CancelRift` (with `RoundFlow.ReturnPlayersToDraft`)
- **Mode:** Debug
- **Side effects:** stops the spawn wait, the watch, and the end timer; parks the KOTH scheduler and turns KOTH off; returns alive players to the watch spot above the next rift (restrained); removes every rift trooper (`npc_trooper`) now and again 5 s and 10 s later; outcome becomes `cancelled`. If the rift had not spawned yet, the next side is unchanged. Replies "No rift is running." when idle
- **Notes:** new in Stage 10. Ends **our** round only: a rift objective that already spawned stays on the map (verified in game; there is no known safe way to remove it). If it is captured later, nothing watches it; `/rift_cleanup` removes its troopers. During a match (`/match_start`) the cancelled round scores no point and the loop continues

#### /rift_cleanup

- **Invocation:** chat `/rift_cleanup` | console `dw_rift_cleanup`
- **Who:** admin
- **Calls:** `RiftService.CleanupRiftTroopers`
- **Mode:** Debug
- **Side effects:** removes every `npc_trooper` on the map (lane troopers are off, so all are rift troopers) and replies with the count
- **Notes:** new in Stage 10; works during or after a rift. Since the 2026-09-27 playtest fixes it removes all troopers, not only those spawned since the last rift started (late-spawned troopers used to pile up)

### Spots (`SpotsPlugin`, in RiftRoulette.dll)

Per-slot spots (`Round/SlotSpots`): each player slot 0-12 has its own watch
spot and its own spot around each team's rift start, offset from the
`RiftRouletteLocations` anchors. Both commands: admin
(`AdminCommand.Authorize`, logged in `Spots`), Debug mode, replies with a
`[Spots]` prefix.

#### /spots_list [green|yellow]

- **Invocation:** chat `/spots_list [side]` | console `dw_spots_list [side]`
- **Who:** admin (server console allowed)
- **Calls:** `SpotCheck.Describe`
- **Mode:** Debug; read-only
- **Side effects:** one console line per slot, for the given side or both: `<SIDE> Slot=N | watch=(x,y,z) | sapphire=(x,y,z) | amber=(x,y,z)`. Error for an unknown side
- **Notes:** new 2026-09-27

#### /spots_walk <watch|sapphire|amber> [green|yellow]

- **Invocation:** chat `/spots_walk <group> [side]` | console `dw_spots_walk <group> [side]`
- **Who:** admin, in game (not the server console; it moves the caller)
- **Calls:** `SpotCheck.Walk` (`MovementService.TeleportTo`, `WatchGuard.Grace`, `WatchSpot.SendUp`)
- **Mode:** Debug
- **Side effects:** teleports the caller to each of the 13 slot spots of the group, one every 1.5 s, and logs where the pawn landed in `spots-*.log`. A spot that moved the pawn more than 32 units (pushed out of a wall, or fell) is a Warning, copied to master. Afterwards sends the caller back up to the watch spot and prints `Walk done: <group> on <SIDE>, N spot(s) moved the pawn.` Side defaults to the current watch-spot side. Refused while a rift round runs or another walk is running; errors for an unknown group or side
- **Notes:** new 2026-09-27; the in-game half of the wall check (the offline half is `scripts/check-spots.py`)

### Match (`GameLoopPlugin`, in RiftRoulette.dll)

All: admin (`AdminCommand.Authorize`; server console trusted; rejected
callers get "You are not allowed to use this command." and a Warning in the
`Match` log with name and Steam ID). Ops run in Debug mode. Results go to
the caller's console with a `[Match]` prefix. Banners go to every player's
screen through the Hud module. Added in Stage 13a.

Since Stage 13d the match also starts and ends by itself
(`AutoStartService`, on by default): the 2nd human player to connect
starts it (Clean mode) and a disconnect that leaves fewer than 2 ends it,
followed 3 s later by a `Waiting for players: ...` chat line. A lone
player who joins gets the same chat line (`Match starts when 1 more player
joins`), repeated every 30 s while they wait (chat, not a banner). It is also
checked 3 s after every DLL load. Turn it off with `/match_auto off` to run
matches by hand.

#### /match_start

- **Invocation:** chat `/match_start` | console `dw_match_start`
- **Who:** admin
- **Calls:** `MatchService.Start` → in Random mode `RandomModeService.BeginMatch`, then each intermission `RandomModeService.PrepareRound` → after each intermission `RoundFlow.RunRound` (the lifecycle path)
- **Mode:** Debug (the loop and its rounds log in detail)
- **Side effects:** resets the score; in Random mode (the default) balances teams once and, at the start of every intermission, swaps every player to a new random hero with a top build; banner `Match starting` / `Round 1 in 5s` (1v1: `1v1` / the pairing); then, forever until `/match_end`: countdown (Random mode: 3 s in, each player sees `<Hero>` / `<build> - 12,345 souls`; 3 s before the round, everyone sees `Round N` / the score, or the pairing in 1v1), start the next rift round (no banner), score the result when it ends (banner `Sapphire 1 - 0 Amber` / `Sapphire took the rift`). If the new rift does not spawn but one is already on the map (left by a cancelled round), that rift is used. A captured rift gives 1 point to the team of the first new rift trooper; tied, cancelled, and timed-out rounds give none. Master log: match started, each round result, match ended
- **Notes:** refuses while a match or a rift is running. Players who die during a round respawn at the watch spot above the rift still being fought, silenced and unable to use items, shoot or melee (they can reload), and are out until the next round. Draft mode: picks carry over; players can `/pick` or `/unpick` during the intermission. Random mode: teams stay; players dead at the start of an intermission get their new hero on respawn. `/rift_start` during a countdown makes the loop wait for that rift instead of starting another

#### /match_end

- **Invocation:** chat `/match_end` | console `dw_match_end`
- **Who:** admin
- **Calls:** `MatchService.End` (`RoundFlow.CancelRound` if a rift is running, `RandomModeService.EndMatch` in Random mode, `DraftService.Reset`, `HudService.AnnounceAll`)
- **Mode:** Debug
- **Side effects:** stops the countdown; cancels a running rift round (a spawned rift objective stays on the map); in Random mode resets each alive player's hero (clears the build) and forgets teams; clears all picks and returns every alive player to the lobby as Skyrunner with zero gold (same as `/draft_reset`); banner `Match over` / final score (1v1: `Best streaks: A 5, B 3`); resets the score (1v1: clears the streak leaderboard). Replies `Match ended after N round(s). Final: ... M player(s) returned to the lobby.` or `No match is running.`
- **Notes:** with auto-start on (the default), a new match starts again on the next connect or disconnect while 2+ players are on; use `/match_auto off` first to keep it ended

#### /match_auto <on|off>

- **Invocation:** chat `/match_auto on` | console `dw_match_auto off`
- **Who:** admin
- **Calls:** `AutoStartService.SetEnabled`; when turned on, `AutoStartService.Check` (→ `MatchService.Start` / `End`)
- **Mode:** Debug
- **Side effects:** turns match auto-start / auto-end on or off (on after every DLL load). Turning it on checks right away, so with 2+ players connected and no match it starts one. Replies `Auto-start on` (plus ` - match started`) or `Auto-start off`
- **Notes:** new in Stage 13d. Turning it off does not end a running match

#### /match_status

- **Invocation:** chat `/match_status` | console `dw_match_status`
- **Who:** admin
- **Calls:** `MatchService.DescribeMatch`
- **Mode:** Debug; read-only
- **Side effects:** two lines: phase (Idle / Intermission / InRound), round, score, ties, auto-start on/off; mode and format, intermission length, rift phase, next side. In 1v1 the first line shows `King=<name> xN` instead of score and ties, followed by the best-streak leaderboard lines

#### /match_intermission <seconds>

- **Invocation:** chat `/match_intermission <seconds>` | console `dw_match_intermission <seconds>`
- **Who:** admin
- **Calls:** `MatchService.SetIntermission`
- **Mode:** Debug
- **Side effects:** sets the seconds between rounds (5-120, default 5; betting opens here and stays open 10 s into the round); applies from the next countdown; lost on plugin reload

#### /match_mode <random|draft|duel|1v1>

- **Invocation:** chat `/match_mode <random|draft|duel|1v1>` | console `dw_match_mode <mode>`
- **Who:** admin
- **Calls:** `MatchConfig.TryParseHeroMode`, `MatchService.SetHeroMode` (→ `ShopAccess.Sync`, `DraftService.Reset`; 1v1: `DuelService.EnterSetup` / `Leave`; `ModeBanner`)
- **Mode:** Debug
- **Side effects:** sets how heroes are chosen for the next match. `random` (default): each intermission everyone gets a new random hero with one of its top 3 builds, draft pool boards hidden, pick commands off. `draft`: the Stage 12 draft with boards. `duel` or `1v1` (Stage 13g): draft off, free hero switching from the menu, 100,000 souls and level 36 for everyone until `/duel_copy`; leaving 1v1 drops the copied build. Buying anywhere (`citadel_allow_purchasing_anywhere`) is on only in 1v1 setup and off in every other mode (the map shops are disabled by CleanSlate). Resets the lobby (everyone alive back to Skyrunner in the draft area, picks cleared, boards redrawn for the mode). Banner to everyone: `Random mode` / `Random hero and build every round`, `1v1 mode` / `Shop open anywhere - build your hero`, or `Draft mode` / `Pick your heroes`. Replies `Mode set to random. N player(s) returned to the lobby.`
- **Notes:** new in Stage 13b. Refused during a match (`/match_end` first; with auto-start on and 2+ players, `/match_auto off` before `/match_end`) and when the mode is unchanged; error for an unknown mode. Resets to `random` on plugin reload

#### /match_format <continuous>

- **Invocation:** chat `/match_format <continuous>` | console `dw_match_format <format>`
- **Who:** admin
- **Calls:** `MatchConfig.TryParseFormat`, `MatchService.SetFormat`
- **Mode:** Debug
- **Side effects:** sets the match format; only `continuous` exists today (rounds loop until `/match_end`). Replies `Format set to continuous.`
- **Notes:** new in Stage 13b; placeholder for best-of formats. Refused during a match

#### /match_config

- **Invocation:** chat `/match_config` | console `dw_match_config`
- **Who:** admin
- **Calls:** `MatchService.DescribeConfig`
- **Mode:** Debug; read-only
- **Side effects:** two lines: `Mode=random | Format=continuous`, then the allowed modes and formats and the intermission length
- **Notes:** new in Stage 13b

### Random (`RandomPlugin`, in RiftRoulette.dll)

Admin (`AdminCommand.Authorize` with the `Random` log; server console
trusted). Debug mode; `[Random]` replies. Added in Stage 13b.

#### /random_status

- **Invocation:** chat `/random_status` | console `dw_random_status`
- **Who:** admin
- **Calls:** `RandomModeService.Describe`
- **Mode:** Debug; read-only
- **Side effects:** a config line (mode, format, assigned, pending, teams, `Bench=` who sits out this round or `none`), then one line per player: slot, name, team, hero, build name and ID, `PENDING` if their swap waits for a respawn, `SITTING OUT` for the bench player

#### /random_reroll

- **Invocation:** chat `/random_reroll` | console `dw_random_reroll`
- **Who:** admin
- **Calls:** `RandomModeService.PrepareRound`
- **Mode:** Debug
- **Side effects:** gives every player a new random hero (never their last one) and build right away, the same as the start of an intermission; dead players are swapped on respawn. Replies `Rerolled: N swapped, M pending`
- **Notes:** error unless the hero mode is `random` and the match is in an intermission. Runs an auto-balance check first, like every intermission. Keeps this intermission's bench player (the bench only rotates at a new intermission)

### Duel (`DuelPlugin`, in RiftRoulette.dll)

Admin (`AdminCommand.Authorize` with the `Duel` log; server console
trusted). Debug mode; `[Duel]` replies. Added in Stage 13g. Used after
`/match_mode 1v1`. Since Stage 13j players join a queue (`/queue`) and the
first two fight, winner stays on; see `user-commands.md`.

#### /duel_copy <slot>

- **Invocation:** chat `/duel_copy <slot>` | console `dw_duel_copy <slot>`
- **Who:** admin
- **Calls:** `DuelService.Copy` (→ `LoadoutService.Capture`, `MatchService.Start` → `DuelService.BeginMatch`)
- **Mode:** Debug (the copy's detail in `duel-*.log` and `loadout-*.log`)
- **Side effects:** captures the slot's exact hero, items (with imbues), ability upgrades, level, ability points and unlocks, then starts the match. The first two in the queue fight on opposite teams and, at the start of every intermission, the two fighters are set to that hero with the copied build and 0 souls. During the match a hero change from the menu kills the player, who respawns with the copy (the death is not counted). Replies `Copied <hero> from <name>. Match started...`
- **Notes:** refused unless the mode is 1v1, no match is running, and at least 2 players are in the queue (the source does not have to be queued); error for an empty slot or a dead player. The build and the queue stay after the match ends, so auto-start continues with them when 2 are queued again

#### /duel_clear

- **Invocation:** chat `/duel_clear` | console `dw_duel_clear`
- **Who:** admin
- **Calls:** `DuelService.ClearSnapshot` (→ `EnterSetup(announce: true)`)
- **Mode:** Debug
- **Side effects:** drops the copied build; players get 100,000 souls again and can switch heroes to build a new one (buying anywhere stays on during setup); each alive player sees `1v1 setup` / `Shop open anywhere - build your hero`
- **Notes:** refused during a match (`/match_end` first; with auto-start on, auto-start will not restart a 1v1 match without a build)

#### /duel_status

- **Invocation:** chat `/duel_status` | console `dw_duel_status`
- **Who:** admin
- **Calls:** `DuelService.Describe`
- **Mode:** Debug; read-only
- **Side effects:** config line with lock state, pending count, queue size and king / streak, the copied build (hero, level, AP, unlocks, ability upgrade bits, item count, source), the item list, then one line per player: slot, name, team, current hero, queue position, `FIGHTER`, `PENDING` if waiting for a respawn

#### /duel_queue

- **Invocation:** chat `/duel_queue` | console `dw_duel_queue`
- **Who:** admin
- **Calls:** `DuelService.DescribeQueue`
- **Mode:** Debug; read-only
- **Side effects:** the next pairing, then the queue in order (`1. Theo (fighting, king, streak 3)`)
- **Notes:** new in Stage 13j

#### /duel_queue_add <slot>

- **Invocation:** chat `/duel_queue_add <slot>` | console `dw_duel_queue_add <slot>`
- **Who:** admin
- **Calls:** `DuelService.JoinQueue`, then `AutoStartService.Check`
- **Mode:** Debug
- **Side effects:** same as the player typing `/queue` (may auto-start the match)
- **Notes:** new in Stage 13j. Error for an empty slot; refused outside 1v1 mode or for a seated admin

#### /duel_queue_remove <slot>

- **Invocation:** chat `/duel_queue_remove <slot>` | console `dw_duel_queue_remove <slot>`
- **Who:** admin
- **Calls:** `DuelService.LeaveQueue(force: true)`, then `AutoStartService.Check`
- **Mode:** Debug
- **Side effects:** takes the player out of the queue, even a fighter between rounds (the next pair then fights after one more intermission). With fewer than 2 queued, auto-start ends the match
- **Notes:** new in Stage 13j. Refused while that player is fighting in a running rift (`/rift_cancel` first)

### Stats (`StatsPlugin`, in RiftRoulette.dll)

Admin (`AdminCommand.Authorize` with the `Stats` log; server console
trusted). Debug mode; `[Stats]` replies. Added in Stage 13c.

#### /stats_board

- **Invocation:** chat `/stats_board` | console `dw_stats_board`
- **Who:** admin
- **Calls:** `StatsService.RefreshBoards`, `StatsService.DescribeAll`
- **Mode:** Debug
- **Side effects:** redraws the Sapphire and Amber stats boards (Random mode: team K/D/A; 1v1 mode: the best-streak leaderboard on both; not in Draft mode; creates them if missing), then lists match running, tracked players, rounds, and one line per player (team, K/D/A, score)

#### /stats_reset

- **Invocation:** chat `/stats_reset` | console `dw_stats_reset`
- **Who:** admin
- **Calls:** `StatsService.Reset`
- **Mode:** Debug
- **Side effects:** zeroes every player's match kills, deaths and assists and refreshes the boards (round counts are kept from the running match). `/match_start` does the same automatically

### Balance (`BalancePlugin`, in RiftRoulette.dll)

Admin (`AdminCommand.Authorize` with the `Balance` log; server console
trusted). Debug mode; `[Balance]` replies. Added in Stage 13c. Auto-balance
itself runs at the start of every Random mode intermission: a stomp (2+
round lead with 8+ more kills and 1.5x the kills) or a 5-round streak, both
counted since the last swap, swaps the winning team's best player (kills +
assists - deaths) with the losing team's weakest (or just moves the best
player when the winning team is bigger). Needs 3+ players; everyone gets a
chat line `Auto-balance: A <-> B`.

#### /balance_status

- **Invocation:** chat `/balance_status` | console `dw_balance_status`
- **Who:** admin
- **Calls:** `BalanceService.Describe`
- **Mode:** Debug; read-only
- **Side effects:** enabled flag and current verdict, rounds / kills / streak since the last swap, the rules

#### /balance_auto <on|off>

- **Invocation:** chat `/balance_auto on` | console `dw_balance_auto off`
- **Who:** admin
- **Calls:** `BalanceService.SetEnabled`
- **Mode:** Debug
- **Side effects:** turns automatic balancing on or off (on after every DLL load). `/balance_now` still works when off

#### /balance_now

- **Invocation:** chat `/balance_now` | console `dw_balance_now`
- **Who:** admin
- **Calls:** `RandomModeService.PrepareRound(forceBalance: true)` (→ `BalanceService.TryBalance`)
- **Mode:** Debug
- **Side effects:** forces a swap now for the leading team (rounds, then kills, since the last swap), then rerolls everyone's hero and build like `/random_reroll`. Replies `Balanced and rerolled: N swapped, M pending`
- **Notes:** error unless the hero mode is `random` and the match is in an intermission. No swap with fewer than 3 players

### Betting (`BettingPlugin`, in RiftRoulette.dll)

Round betting runs by itself in Random mode (see `user-commands.md` `/bet`):
100 starting chips, 100 per kill, all-in bets on the next round during the
intermission, a win doubles the stake, a `BETTING` board on the empty side
of the watch spot. Admin (`AdminCommand.Authorize` with the `Betting` log;
server console trusted). `[Betting]` replies.

#### /bet_status

- **Invocation:** chat `/bet_status` | console `dw_bet_status`
- **Who:** admin
- **Calls:** `BettingService.RefreshBoard`, `BettingService.Describe`
- **Mode:** Debug; read-only apart from redrawing the board
- **Side effects:** redraws the betting board (Random mode), then one line with active, open, bet count, total staked, and one line per participant: slot, name, chips, open bet
- **Notes:** new 2026-09-27

### Loadout (`LoadoutPlugin`, in RiftRoulette.dll)

Admin (`AdminCommand.Authorize` with the `Loadout` log; server console
trusted). Debug mode; `[Loadout]` replies. Module commands (game-agnostic).
Added in Stage 13b.

#### /loadout_give <slot> <hero> [build]

- **Invocation:** chat `/loadout_give <slot> <hero> [build]` | console `dw_loadout_give <slot> <hero> [build]`
- **Who:** admin
- **Calls:** `HeroBuildCatalog.TryParseHero` / `BuildsFor`, `LoadoutService.Swap` (→ `Apply`)
- **Mode:** Debug (per-ability and per-item detail in `loadout-*.log`)
- **Side effects:** swaps the player in that slot to the hero (enum name like `inferno` or game name like `Infernus`), then 1 s later plans the build's first 9 items in order (every required item plus one random item per optional group; Monster Rounds, Cultist Sacrifice and Golden Goose Egg skipped; upgrades replace their components; the most expensive items dropped while the total is over 20,000 souls), resets the hero, sets the level a real hero has at that item value (Deadlock's soul table: for example 18,000 souls is level 24 with 4 unlocks and 20 ability points), applies the build's ability order only as far as those unlocks and points pay for (tiers cost 1, 2, 5; it stops at the first step that does not fit), grants the items with imbues, sets souls, ability points and unlocks to 0, and heals to full. The `Loadout applied` line in `loadout-*.log` shows `Level`, `Boons`, `Unlocks`, `Points`, `PointsLeft`, `Steps` / `StepsTotal` and the ranks set. `build` is 1-3; 0 or omitted picks one at random
- **Notes:** test tool. Errors: empty slot, unknown hero, no builds, bad build number, dead player. In Draft mode, Draft's hero enforcement switches a player without a matching pick back to their pick or Skyrunner, so use it in Random mode or on a player whose pick is that hero

#### /loadout_copy <from> <to>

- **Invocation:** chat `/loadout_copy <from> <to>` | console `dw_loadout_copy <from> <to>`
- **Who:** admin
- **Calls:** `LoadoutService.Capture`, `LoadoutService.SwapSnapshot` (→ `ApplySnapshot`)
- **Mode:** Debug
- **Side effects:** copies the exact hero, items (with imbues), ability upgrades, level, ability points and unlocks of the player in slot `from` onto the player in slot `to` (hero swap, then 1 s later the copy; souls set to 0). No cap and no banned-item filter. Replies with a one-line summary of the copy
- **Notes:** new in Stage 13g; test tool for 1v1 mode. Errors: empty slot, source dead, target dead. Draft's hero enforcement may undo it in Draft mode (use it in 1v1 or Random mode)

#### /loadout_list <hero>

- **Invocation:** chat `/loadout_list <hero>` | console `dw_loadout_list <hero>`
- **Who:** admin
- **Calls:** `HeroBuildCatalog.BuildsFor` / `PlannedValue`, `LoadoutPlanner.ItemOrder` / `FirstSlots`
- **Mode:** Debug; read-only
- **Side effects:** lists the hero's stored builds (name, build ID, rank, matches, wins, planned value, optional groups), each followed by the 9 items it grants with the first pick of each optional group (before the 20,000 cap)

#### /loadout_info

- **Invocation:** chat `/loadout_info` | console `dw_loadout_info`
- **Who:** admin
- **Calls:** `HeroBuildCatalog.Default`
- **Mode:** Debug; read-only
- **Side effects:** two lines: when the data was fetched, source, window (days), hero count; then the baseline value (median planned value of all builds), the cap (20,000), and the banned items

### Restraint (`RestraintPlugin`, in RiftRoulette.dll)

Admin (`AdminCommand.Authorize` with the `Restraint` log; server console
trusted). Debug mode; `[Restraint]` replies; `restraint-*.log`. Module
commands (game-agnostic). Added in Stage 13h. The game also restrains
everyone it sends up top and releases the fighters it moves into the rift.

#### /restrain <slot>

- **Invocation:** chat `/restrain <slot>` | console `dw_restrain <slot>`
- **Who:** admin
- **Calls:** `RestraintService.Restrain`
- **Mode:** Debug
- **Side effects:** silences, blocks item actives, shooting and melee for the player (game modifier `modifier_citadel_silenced` plus the `Silenced`, `ItemsDisabled`, `ShootingDisabled`, `MeleeDisabled`, `IgnoredByNpcTargeting` states set every frame) until released; while restrained the player takes no damage (`GameLoopPlugin.OnTakeDamage`); reloading still works (no disarm, which also blocks reloading); survives death and hero swaps
- **Notes:** error for an empty slot; replies `was already restrained` if so

#### /restrain_release <slot>

- **Invocation:** chat `/restrain_release <slot>` | console `dw_restrain_release <slot>`
- **Who:** admin
- **Calls:** `RestraintService.Release`
- **Mode:** Debug
- **Side effects:** removes both modifiers and turns the states off
- **Notes:** the game restrains the player again the next time it sends them up top

#### /restrain_list

- **Invocation:** chat `/restrain_list` | console `dw_restrain_list`
- **Who:** admin
- **Calls:** `RestraintService.Describe`
- **Mode:** Debug; read-only
- **Side effects:** restrained count, the modifier and state names, then per restrained player: slot, name, alive, which restraint modifiers are active

#### /status_add <slot> <modifier> [seconds]

- **Invocation:** chat `/status_add <slot> <modifier> [seconds]` | console `dw_status_add ...`
- **Who:** admin
- **Calls:** `RestraintService.AddModifier` (`pawn.AddModifier` with a `duration`)
- **Mode:** Debug; Information line in `restraint-*.log`
- **Side effects:** adds any game modifier by name for `seconds` (default 10), e.g. `/status_add 3 boss_victim_no_melee 30`. Replies `added` or `refused (unknown name?)`
- **Notes:** for testing modifier names in game; error for an empty slot or a dead pawn

#### /status_remove <slot> <modifier>

- **Invocation:** chat `/status_remove <slot> <modifier>` | console `dw_status_remove ...`
- **Who:** admin
- **Calls:** `pawn.RemoveModifier`
- **Mode:** Debug
- **Side effects:** removes that modifier from the player's pawn; replies `removed` or `not found`

### Hud (`HudPlugin`, in RiftRoulette.dll)

#### /hud_announce <title> [| description]

- **Invocation:** chat `/hud_announce <title> [| description]` | console `dw_hud_announce ...`
- **Who:** admin (`AdminCommand.Authorize` with the `Hud` log; server console trusted)
- **Calls:** `HudService.ParseAnnouncement`, `HudService.AnnounceAll`
- **Mode:** Debug
- **Side effects:** shows the game's on-screen announcement banner to every player; text after the first `|` is the smaller description line, e.g. `/hud_announce Round 3 | GREEN rift`. Replies `[Hud] Announced to N player(s)`
- **Notes:** new in Stage 13a, for trying banner text by hand. Module command: game-agnostic

#### /hud_say <message>

- **Invocation:** console `dw_hud_say <message>` (client Deadworks console or server console; works while spectating, where chat is unavailable) | chat `/hud_say <message>`
- **Who:** admin (`AdminCommand.Authorize` with the `Hud` log; server console trusted)
- **Calls:** `HudService.AnnounceAll(message, HudService.AdminSayLabel)`
- **Mode:** Debug
- **Side effects:** shows the whole message as the big banner title on every player's screen, with `Server admin` as the smaller line below. Logs `Announced to all Title=<message> ...` in `hud-*.log`. Replies `[Hud] Said to N player(s): <message>`; errors on an empty message
- **Notes:** new 2026-09-27, for talking to players. `|` stays part of the text. The console splits commands on `;`, so wrap a message containing `;` in quotes. Banners show for about 3 s and queue when sent close together. Module command: game-agnostic

### Session (`SessionPlugin`, in RiftRoulette.dll)

#### /session_info

- **Invocation:** chat `/session_info` | console `dw_session_info`
- **Who:** admin (`AdminCommand.Authorize`; calls logged in `session-*.log`)
- **Calls:** `BublockLog.SessionId`, `RoundId`, `Directory`, `Hub.FileLoggingEnabled`; `Server.MapName`
- **Mode:** read-only
- **Side effects:** prints session id, round id (`-` when none), map, RiftRoulette log folder, and file logging on/OFF to the caller's console

### SelfTest (`SelfTestPlugin`, in RiftRoulette.dll)

Patch-day checks; see `reference/patch-day.md`.

#### /selftest_run [all]

- **Invocation:** chat `/selftest_run [all]` | console `dw_selftest_run [all]`
- **Who:** admin (`AdminCommand.Authorize`; server console trusted)
- **Calls:** `SelfTestService.Run(Debug)`
- **Mode:** Debug; read-only
- **Side effects:** checks convars, the KOTH schema fields and gamerules pointer, entity names, map rift points, hero and item data (`ItemInfo.Exists`), floor under every slot spot (`Trace.Ray`), player controllers and hook counters. Replies WARN / FAIL lines plus per-area and total counts (`all`: every check). Every result goes to `selftest-*.log`; WARN / FAIL lines and the summary also reach the master log

#### /selftest_live <slot>

- **Invocation:** chat `/selftest_live <slot>` | console `dw_selftest_live <slot>`
- **Who:** admin (`AdminCommand.Authorize`; server console trusted)
- **Calls:** `SelfTestService.Live` → `LoadoutService.Capture`, `HudService.Announce`, `WatchSpot.SendUp`, then `RestraintService.Release` if the player was not restrained before
- **Mode:** Debug
- **Side effects:** sends a `Self-test` banner to that player, teleports them to their own watch spot and restrains them; 1 s later replies Loadout / Hud / Teleport / Restraint results (modifier and each state). Refused during a rift round or when the player has no living hero; error when the slot is empty

### DevTools (`DevToolsPlugin`, DevTools.dll)

All seven: admin (`AdminCommand.Authorize`; server console trusted except
`/dev_herowatch`, which needs a player; rejected callers get "You are not
allowed to use this command." and a Warning in `DevTools/commands` with name
and Steam ID). Results go to the caller's console (server console for a null
caller) with a `[DevTools]` prefix. Diagnostic tools with no lifecycle
caller, so there is no Clean/Debug split. Renamed from the archive in
Stage 12 (old names removed); the archive left four of them ungated.
DevTools also logs each game modifier name the first time it is added
(`ModifierProbe`, `DevTools/modifiers-*.log`); no command.

#### /dev_logpath

- **Invocation:** chat `/dev_logpath` | console `dw_dev_logpath`
- **Who:** admin
- **Calls:** `LogPaths.ResolveRoot()`, `BublockLog.Directory`, `BublockLog.SessionId`, `BublockLog.Hub.FileLoggingEnabled`
- **Mode:** read-only
- **Side effects:** prints `Log root`, DevTools log folder, session id, and file-logging on/OFF
- **Notes:** was `logpath` (added in Stage 4; not in the archive). After the Stage 12 push, confirms the server log path that `scripts/pull-logs.sh` mirrors (`/server/game/bin/win64/bublock/logs/`)

#### /ent_find <filter>

- **Invocation:** chat `/ent_find <filter>` | console `dw_ent_find <filter>`
- **Who:** admin
- **Calls:** `Entities.All` scan
- **Mode:** read-only
- **Side effects:** prints index, designer name, class name, and name for every entity whose combined text contains the filter (case-insensitive)
- **Notes:** archive `entities` (printed to the server console)

#### /ent_info <designerName>

- **Invocation:** chat `/ent_info <designerName>` | console `dw_ent_info <designerName>`
- **Who:** admin
- **Calls:** `Entities.ByDesignerName`
- **Mode:** read-only
- **Side effects:** prints name, class, index, handle, validity, position, velocity, team, health, max health, life state, alive, ground state, and VData/modifier/body presence for each match
- **Notes:** archive `entity_info` (ungated, server console, `[Rift Wars]` headers)

#### /ent_remove <designerName>

- **Invocation:** chat `/ent_remove <designerName>` | console `dw_ent_remove <designerName>`
- **Who:** admin
- **Calls:** `Entities.ByDesignerName`, `entity.Remove()`
- **Mode:** destructive
- **Side effects:** removes every entity with that designer name; prints and logs (`DevTools/commands`) each one, then a count
- **Notes:** archive `entity_remove` (ungated). Never use on `info_super_trooper_spawn` (crash risk); the command does not block it

#### /dev_herowatch

- **Invocation:** chat `/dev_herowatch` | console `dw_dev_herowatch` (player callers only)
- **Who:** admin, in game
- **Calls:** `Timer.Every(0.5 s)` reading the caller's `HeroID`
- **Mode:** diagnostic
- **Side effects:** toggles a watcher that prints `HERO CHANGE` to the caller's console and logs it in `DevTools/herowatch` each time their hero id changes; cancelled on plugin unload
- **Notes:** archive `herowatch`

#### /ent_snapshot

- **Invocation:** chat `/ent_snapshot` | console `dw_ent_snapshot`
- **Who:** admin
- **Calls:** `Entities.All`
- **Mode:** read-only
- **Side effects:** stores index, designer, class, and name of every entity in memory (lost on reload); prints the count
- **Notes:** archive `snapshot` (ungated)

#### /ent_diff

- **Invocation:** chat `/ent_diff` | console `dw_ent_diff`
- **Who:** admin
- **Calls:** `Entities.All` vs the stored snapshot
- **Mode:** read-only
- **Side effects:** prints entities added and removed since `/ent_snapshot` (or a hint if no snapshot exists)
- **Notes:** archive `compare` (ungated)

### CleanSlate (`CleanSlatePlugin`, CleanSlate.dll)

#### /cleanup_run

- **Invocation:** chat `/cleanup_run` | console `dw_cleanup_run`
- **Who:** admin (`AdminCommand.Authorize`; server console trusted; calls logged in `CleanSlate/cleanup`)
- **Calls:** `CleanSlateService.ApplyConvars`, `CleanSlateService.RemoveMapEntities`
- **Mode:** Debug (startup and hot reload run the same ops Clean)
- **Side effects:** sets `citadel_trooper_spawn_enabled 0`, `citadel_npc_spawn_enabled 0`, `citadel_active_lane 0`, `citadel_midboss_initial_spawn_time_override 999999`, and the urn off: `citadel_crate_spawn_enabled 0`, `citadel_crate_disable_early_spawn 1`, `citadel_crate_spawn_initial_delay 999999`, `citadel_crate_respawn_interval 999999`; immediately removes every `npc_trooper_boss`, `npc_boss_tier2`, `npc_barrack_boss`, `citadel_item_powerup_spawner`, `citadel_herotest_orbspawner`, `citadel_shop_prop_dynamic` (shop kiosks) and sends `Disable` to every `trigger_item_shop` / `trigger_item_shop_safe_zone` (shop buy zones); `cleanup` summary with counts per classname; master line `Map cleanup re-run Removed=[...] Disabled=[...]`; no banner; replies `[CleanSlate] Convars applied, N entities removed, M disabled`
- **Notes:** new in Stage 12, for example after a map change without a restart. Never removes `info_super_trooper_spawn` or the urn spawn points (`item_crate_spawn`); buy zones are disabled, not removed. Does not touch buying (`citadel_allow_purchasing_anywhere` belongs to RiftRoulette)
