# LobbyPlugin

Thin plugin class (Name `Rift Roulette Lobby`) for player connection
lifecycle, server setup, and the lobby commands. Ops live on
`LobbyService`. Extracted from Legacy in Stage 8.

## Hooks (Clean mode)

| Hook | Does |
|---|---|
| `OnLoad(isReload: true)` | `LobbyService.ApplyServerConvars()` (a hot reload skips `OnStartupServer`; keeps `maxplayers 13` and the buying convar right after an upload), then `AdminSeat.Restore()` (an admin still on the observer pawn goes back into the seat), then `BanStatueService.KickConnectedBanned()` (the reload dropped statue state and kick timers, so banned players still connected are kicked) |
| `OnLoad` (every load) | `Timer.Every(StreamCam.TickSeconds, StreamCam.Tick)`: the stream camera check every 2 s; `Timer.Every(AutoStartService.WaitingReminderSeconds, RemindWaiting)`: the waiting chat line every 30 s while a lone player waits for a match; `Timer.Every(BanStatueService.SustainSeconds, Sustain)`: keeps statues restrained and re-adds the statue modifier every 1 s (timers die on hot reload, so all start here) |
| `OnLoad` (every load) | Hooks the two incoming pause net messages, `CCLCMsg_RequestPause` and `CCitadelClientMsg_Pause` (`NetMessages.HookIncoming`); each returns `Stop` when `PauseGuard.Block(SenderSlot, "message", <type>)` says so. A message type without a registered ID logs a Warning in `Lobby` and is skipped. The handles are kept for `OnUnload` |
| `OnUnload` | Cancels the pause message hooks, so a hot reload does not stack them |
| `OnPrecacheResources` | `Precache.AddHero(BanStatueService.StatueLookHero)` (Vyper), logs `Precached hero` in `Lobby`. Runs at map load only, not on hot reload |
| `OnStartupServer` | `LobbyService.ApplyServerConvars()` |
| `OnGameFrame` | `PauseGuard.Tick()` every frame, simulating or not (a paused game does not simulate): the automatic unpause while pausing is off |
| `OnClientConCommand` | First `StreamCam.OnAdminCommand(controller, command, args)` (seated admins only: logged in `spectate-*.log`; a `spec_*` command pauses the stream camera). Then a `PauseRule.IsPauseCommand` command (`pause`, `setpause`, `citadel_pause`, `citadel_toggle_server_pause`) returns `Stop` when `PauseGuard.Block(controller, "command", <name>)` says so; everything else `Continue` |
| `OnClientConnect` | Returns `AccessService.AllowConnect(SteamId, Name) && AdminSeat.AllowConnect(SteamId, Name)`. Access first: a banned ID gets a statue visit or is refused inside a rejoin lockout (`BanStatueService.AdmitBanned`), and in private mode anyone neither whitelisted nor admin is refused. Then the seat rule: `false` refuses a non-admin once 12 participants are playing (the 13th connection is the admin seat) |
| `OnClientFullConnect` | When the controller is present: a banned player let in at connect (`BanStatueService.TakeArrival`) becomes a statue and is kicked 10 s later (`Petrify(RejoinKickSeconds, liveBan: false)`); otherwise every admin is seated as a spectator (`AdminSeat.Sit`; `dw_seat_play` to play); everyone else goes through `LobbyService.AdmitPlayer(controller, Timer)` (no bot check, as the archive), which places the player on the smaller team and checks auto-start 2 s later |
| `OnClientDisconnect` | `LobbyService.RemovePlayer(controller, Timer)` when the controller is present (may auto-end the match, never auto-starts one) |
| `player_spawn` | Skips bots and seated admins (`Participants.IsParticipant`, but statues pass so a respawned statue goes back up); gives `WatchGuard.Grace` at once (a respawn at base is not a rescue) and `StreamCam.NoteSpawn` (the camera waits 5 s before following a fresh hero), then on the next tick `WatchSpot.SendUp(player)`: restrained and teleported to the watch spot above the rift being fought, or the next one when idle (archive `ReturnToDraftOnSpawn`; stays here because it needs `Timer`) |
| `player_death` | `LobbyService.LogDeath` when controller and pawn are present; then `StreamCam.OnDeath(victim, attacker, Timer)` (the camera cuts to the killer if the admin was watching the victim) |
| `player_used_ability` | Resolves the caster from `Player` (or `Caster`) pawn's controller. The first time each ability name is seen since load it logs Information `Ability name seen for the first time since load Ability= Big= Caster=` (confirms the event fires and shows real names); every use is Trace. A `BigUlts.IsBig` ability cast by a participant goes to `StreamCam.OnBigUlt` |

## Commands

| Command | Who | Does |
|---|---|---|
| `/status` | player (in game) | `DescribePlayer` for the caller, sent to them in chat and logged at Information in `Players` |
| `/commands` | player (in game) | `CommandList.PlayerCommands` for this assembly, one chat line each, then `Full list: dw_help in console` |
| `/player_list` | admin | count, then `DescribePlayer` for every player, to the caller's console |
| `/player_info <slot>` | admin | `DescribePlayer` for one slot; also logged in `Players` |
| `/player_kick <slot>` | admin | `LobbyService.KickPlayer`; error if the slot is empty |
| `/player_team <slot> <sapphire\|amber>` | admin | `LobbyService.SetTeam`; errors for an unknown team, empty slot, or a player with a pick |
| `/lobby_setup` | admin | `LobbyService.ApplyServerConvars(Debug)` |
| `/pause_allow [on\|off]` | admin | No argument: `PauseGuard.Describe` (on/off, paused state, counts). `on` / `off` (or `1` / `0`): `PauseGuard.SetAllowed(Debug)`, which sets the pause convars and writes a master line; error for another value. Resets to off on every load |
| `dw_seat_spec` | admin, console only (`ConsoleOnly`; chat `/seat_spec` does not run) | `AdminSeat.Sit`: the admin moves to the spectator seat (`MakeObserver` on the next tick); works any time, including mid-round |
| `/seat_play` | admin | `AdminSeat.Stand`: the admin goes back onto a team through `AdmitPlayer`; refused when 12 are playing |
| `/seat_status` | admin | `AdminSeat.Describe` |
| `/spec_auto <on\|off>` | admin | `StreamCam.SetAuto`: the automatic stream camera on or off (default on; also ends a manual hold); error for another value |
| `/spec_status` | admin | `StreamCam.Describe`: auto, manual hold, seated, observer mode, who is on camera, parked side, top-down state, return-to player, round and whether its top-down is used |
| `/spec_overview` | admin | `StreamCam.ShowOverview`: top-down over the rift now for 10 s; does not use the round's automatic top-down; error when not spectating |

Seat and `spec_*` commands take no player argument. They target the caller, or without a
caller (server console, or a client console command that arrived without
one, as `dw_seat_play` did at 09:39) the first connected player whose Steam
ID is in `AdminAuth.SteamIds`; `The admin is not connected.` if none.
Spectators cannot type in game chat, so a seated admin uses the client
console (`dw_seat_play`) or the server console.

The archive names `/state`, `/kick`, and `/test` (now `/mv_tp draft`) were
removed in Stage 12; `/player_kick` replaces the ungated archive `/kick`.

Admin commands call `AdminCommand.Authorize` first (server console trusted;
accepted and rejected calls logged in `Lobby`), run ops in Debug mode, and
reply with `AdminCommand.Reply`. Errors use `CommandException`.

## Invariants

- `OnClientConnect`, `OnClientFullConnect`, `OnClientDisconnect`,
  `player_spawn` and `player_death` each call `SelfTest/EventCounters.Hit`
  first (`client_connect`, `client_full_connect`, `client_disconnect`,
  `player_spawn`, `player_death`, `player_used_ability`), so `dw_selftest_run` can tell whether the
  hook still fires after a game update.

- Hooks never throw; missing controllers or pawns are skipped.
- State: `SeenAbilities` (ability names already logged since load) and the
  pause message hook handles; picks live in `Draft/DraftState`, camera state
  in `StreamCam`, pause state in `PauseGuard`.
