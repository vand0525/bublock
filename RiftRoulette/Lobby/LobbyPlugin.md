# LobbyPlugin

Thin plugin class (Name `Rift Roulette Lobby`) for player connection
lifecycle, server setup, and the lobby commands. Ops live on
`LobbyService`.

## Hooks (Clean mode)

| Hook | Does |
|---|---|
| `OnLoad(isReload: true)` | `LobbyService.ApplyServerConvars()` (a hot reload skips `OnStartupServer`; keeps `maxplayers 13` and the buying convar right after an upload), then `AdminSeat.Restore(Timer)` (every connected admin goes back into the seat; one on a hero pawn keeps roaming and is re-cloaked; then roam vs spectate is synced), then `BanStatueService.KickConnectedBanned()` (the reload dropped statue state and kick timers, so banned players still connected are kicked) |
| `OnLoad` (every load) | `Timer.Every(StreamCam.TickSeconds, StreamCam.Tick)`: the stream camera check every 2 s; `Timer.Every(AutoStartService.WaitingReminderSeconds, RemindWaiting)`: the waiting chat line every 30 s while a lone player waits for a match; `Timer.Every(BanStatueService.SustainSeconds, Sustain)`: keeps statues restrained and re-adds the statue modifier every 1 s; `Timer.Every(AutoRestartService.CheckSeconds, Check)`: the automatic map reload check every 60 s (timers die on hot reload, so all start here) |
| `OnLoad` (every load) | Hooks the two incoming pause net messages, `CCLCMsg_RequestPause` and `CCitadelClientMsg_Pause` (`NetMessages.HookIncoming`); each returns `Stop` when `PauseGuard.Block(SenderSlot, "message", <type>)` says so. A message type without a registered ID logs a Warning in `Lobby` and is skipped. The handles are kept for `OnUnload` |
| `OnUnload` | Cancels the pause message hooks, so a hot reload does not stack them |
| `OnPrecacheResources` | `Precache.AddHero(BanStatueService.StatueLookHero)` (Vyper), logs `Precached hero` in `Lobby`. Runs at map load only, not on hot reload |
| `OnStartupServer` | Every map start (also after a map reload): `AutoRestartService.OnMapStart()` (join watch reset), `AdminSeat.ResetForMap()` (seats cleared; admins are seated again when they reconnect), then `LobbyService.ApplyServerConvars()` |
| `OnGameFrame` | `PauseGuard.Tick()` every frame, simulating or not (a paused game does not simulate): the automatic unpause while pausing is off |
| `OnClientConCommand` | A `PauseRule.IsPauseCommand` command (`pause`, `setpause`, `citadel_pause`, `citadel_toggle_server_pause`) returns `Stop` when `PauseGuard.Block(controller, "command", <name>)` says so; everything else `Continue` |
| `OnClientConnect` | First `Participants.ClearLeaving(SteamId)` (a reconnecting player is a participant again). Returns `AccessService.AllowConnect(SteamId, Name) && AdminSeat.AllowConnect(SteamId, Name)`, and passes the result to `AutoRestartService.OnConnect` (an allowed connection is watched until its full connect). Access first: a banned ID gets a statue visit or is refused inside a rejoin lockout (`BanStatueService.AdmitBanned`), and in private mode anyone neither whitelisted nor admin is refused. Then the seat rule: `false` refuses a non-admin once 12 participants are playing (the 13th connection is the admin seat) |
| `OnClientFullConnect` | First `AutoRestartService.OnFullConnect(Slot, controller)` (join completed). When the controller is present: a banned player let in at connect (`BanStatueService.TakeArrival`) becomes a statue and is kicked 10 s later (`Petrify(RejoinKickSeconds, liveBan: false)`); otherwise every admin is seated as a spectator (on a map-change reconnect, `IsMapChangeReconnect`, the old seat is forgotten first so `Sit` runs in full; `AdminSeat.Sit`; `dw_seat_play` to play, `/seat_roam` to roam); everyone else goes through `LobbyService.AdmitPlayer(controller, Timer)` (no bot check), which places the player on the smaller team and checks auto-start 2 s later |
| `OnClientDisconnect` | First `AutoRestartService.OnDisconnect(Slot, IsMapChange)` (a join that never completed counts as stuck). A map-change disconnect (`args.IsMapChange`, reason `NetworkDisconnectShutdown`) only logs Debug `Map change disconnect, player kept`: Deadworks keeps the player, who reconnects to the next map. Otherwise `LobbyService.RemovePlayer(controller, Timer)` when the controller is present (may auto-end the match, never auto-starts one), else `LobbyService.OnDisconnectWithoutController(Slot, Reason, Timer)` |
| `player_spawn` | A roaming admin (`AdminSeat.IsRoaming`) gets `AdminSeat.PlaceAndCloak` on the next tick (in front of the welcome sign, cloak, no restraint) and nothing else. Otherwise skips bots and seated admins (`Participants.IsParticipant`, but statues pass so a respawned statue goes back up); gives `WatchGuard.Grace` at once (a respawn at base is not a rescue) and `StreamCam.NoteSpawn` (the camera waits 5 s before following a fresh hero), then on the next tick `WatchSpot.SendUp(player)`: restrained and teleported to the watch spot above the rift being fought, or the next one when idle (runs here because it needs `Timer`) |
| `player_death` | `LobbyService.LogDeath` when controller and pawn are present; then `StreamCam.OnDeath(victim, attacker, Timer)` (the camera cuts to the killer if the admin was watching the victim) |

## Commands

| Command | Who | Does |
|---|---|---|
| `/status` | player (in game) | `DescribePlayer` for the caller, sent to them in chat and logged at Information in `Players` |
| `/commands` | player (in game) | `CommandList.PlayerCommands` for this assembly, one chat line each, then `Full list: dw_help in console` |
| `/about` | player (in game) | `AboutText.Lines(BettingService.LingerSeconds)`, one chat line each: the mode and how betting works. Not logged |
| `/player_list` | admin | count, then `DescribePlayer` for every player, to the caller's console |
| `/player_info <slot>` | admin | `DescribePlayer` for one slot; also logged in `Players` |
| `/player_kick <slot>` | admin | `LobbyService.KickPlayer`; error if the slot is empty |
| `/player_team <slot> <sapphire\|amber>` | admin | `LobbyService.SetTeam`; errors for an unknown team, empty slot, or a player with a pick |
| `/lobby_setup` | admin | `LobbyService.ApplyServerConvars(Debug)` |
| `/lobby_flex` | admin | `FlexSlots.UnlockAll(Debug)`, replies with the team count, then `FlexSlots.Describe()` (each team's flex slot flags; 15 = all open) |
| `/pause_allow [on\|off]` | admin | No argument: `PauseGuard.Describe` (on/off, paused state, counts). `on` / `off` (or `1` / `0`): `PauseGuard.SetAllowed(Debug)`, which sets the pause convars and writes a master line; error for another value. Lasts until the next load or `/access_mode` change, which set pausing from the private flag |
| `dw_seat_spec` | admin, console only (`ConsoleOnly`; chat `/seat_spec` does not run) | `AdminSeat.Sit`: the admin moves to the spectator seat (`MakeObserver` on the next tick); works any time, including mid-round; a roaming admin stops roaming and spectates |
| `/seat_play` | admin | `AdminSeat.Stand`: the admin goes back onto a team through `AdmitPlayer`; refused when 12 are playing |
| `/seat_roam` | admin | `AdminSeat.RoamNow`: roam now (invisible Abrams in front of the welcome sign), or back in front of the sign if already roaming; refused only when not seated |
| `/seat_status` | admin | `AdminSeat.Describe` |
| `/restart_status` | admin | `AutoRestartService.Describe`: on / off, map uptime, stuck joins, joins in progress, join budget (fighter-rounds / budget, rounds left) |
| `/restart_now` | admin | `AutoRestartService.Restart("admin", Debug)`: reloads the map now, even with players (they reconnect by themselves) |
| `/restart_auto <on\|off>` | admin | `AutoRestartService.SetEnabled(Debug)` until the next load; error for another value |
| `/restart_budget [n]` | admin | No argument: `MapRefreshService.Describe`. A number: `MapRefreshService.SetBudget(n, Debug)` (fighter-rounds before the join budget reload, 0 = off) until the next load; error for a negative or non-number |
| `/spec_auto <on\|off>` | admin | `StreamCam.SetAuto`: the automatic stream camera on or off (default on; resets that admin's camera state); error for another value |
| `/spec_status` | admin | `StreamCam.Describe`: auto, seated, observer mode, fly cam, the view angle read; who is on camera, parked side, placed / adjusting, watch side; the saved framing per side |
| `/spec_reset` | admin | `StreamCam.ResetFraming(Debug)`: forgets the saved framing (`streamcam.json`), so the next park is the top-down default |

Seat and `spec_*` commands take no player argument. They target the caller, or without a
caller (server console, or a client console command that arrived without
one, as `dw_seat_play` did at 09:39) the first connected player whose Steam
ID is in `AdminAuth.SteamIds`; `The admin is not connected.` if none.
Spectators cannot type in game chat, so a seated admin uses the client
console (`dw_seat_play`) or the server console.

Admin commands call `AdminCommand.Authorize` first (server console trusted;
accepted and rejected calls logged in `Lobby`), run ops in Debug mode, and
reply with `AdminCommand.Reply`. Errors use `CommandException`.

## Invariants

- `OnClientConnect`, `OnClientFullConnect`, `OnClientDisconnect`,
  `player_spawn` and `player_death` each call `SelfTest/EventCounters.Hit`
  first (`client_connect`, `client_full_connect`, `client_disconnect`,
  `player_spawn`, `player_death`), so `dw_selftest_run` can tell whether the
  hook still fires after a game update.

- Hooks never throw; missing controllers or pawns are skipped.
- Build against the server's `DeadworksManaged.Api.dll`. A hook that uses an
  API member whose type changed on the server (Deadworks v0.4.18 made
  `ClientDisconnectedEvent.Reason` an `ENetworkDisconnectionReason` enum)
  fails before its first line on every call, and only the host console
  shows it: from 2026-09-28 06:01 to the fix, no disconnect was cleaned up.
- State: the pause message hook handles; picks live in `Draft/DraftState`, camera state
  in `StreamCam`, pause state in `PauseGuard`.
