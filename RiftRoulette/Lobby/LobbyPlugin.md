# LobbyPlugin

Thin plugin class (Name `Rift Roulette Lobby`) for player connection
lifecycle, server setup, and the lobby commands. Ops live on
`LobbyService`. Extracted from Legacy in Stage 8.

## Hooks (Clean mode)

| Hook | Does |
|---|---|
| `OnLoad(isReload: true)` | `LobbyService.ApplyServerConvars()` (a hot reload skips `OnStartupServer`; keeps `maxplayers 13` and the buying convar right after an upload) |
| `OnStartupServer` | `LobbyService.ApplyServerConvars()` |
| `OnClientConnect` | Returns `AccessService.AllowConnect(SteamId, Name) && AdminSeat.AllowConnect(SteamId, Name)`. Access first: banned IDs are refused, and in private mode anyone neither whitelisted nor admin. Then the seat rule: `false` refuses a non-admin once 12 participants are playing (the 13th connection is the admin seat) |
| `OnClientFullConnect` | When the controller is present: every admin is seated as a spectator (`AdminSeat.Sit`; `dw_seat_play` to play); everyone else goes through `LobbyService.AdmitPlayer(controller, Timer)` (no bot check, as the archive), which places the player on the smaller team and checks auto-start 2 s later |
| `OnClientDisconnect` | `LobbyService.RemovePlayer(controller, Timer)` when the controller is present (may auto-end the match, never auto-starts one) |
| `player_spawn` | Skips bots and seated admins (`Participants.IsParticipant`); gives `WatchGuard.Grace` at once (a respawn at base is not a rescue), then on the next tick `WatchSpot.SendUp(player)`: restrained and teleported to the watch spot above the rift being fought, or the next one when idle (archive `ReturnToDraftOnSpawn`; stays here because it needs `Timer`) |
| `player_death` | `LobbyService.LogDeath` when controller and pawn are present |

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
| `dw_seat_spec` | admin, console only (`ConsoleOnly`; chat `/seat_spec` does not run) | `AdminSeat.Sit`: the admin moves to the spectator seat (`MakeObserver` on the next tick); works any time, including mid-round |
| `/seat_play` | admin | `AdminSeat.Stand`: the admin goes back onto a team through `AdmitPlayer`; refused when 12 are playing |
| `/seat_status` | admin | `AdminSeat.Describe` |

Seat commands take no arguments. They target the caller, or without a
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
  `player_spawn`, `player_death`), so `dw_selftest_run` can tell whether the
  hook still fires after a game update.

- Hooks never throw; missing controllers or pawns are skipped.
- No state of its own; picks live in `Draft/DraftState`.
