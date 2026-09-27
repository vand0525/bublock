# Lobby — Feature

## Purpose

Everything about players being on the server outside the draft and rift
logic: server convars at startup, admitting players to the draft area on
connect, returning them to draft on spawn, cleaning up on disconnect, kick,
team moves, and player status. Extracted from Legacy in Stage 8.

Stage 13c: new connections go to the smaller team (`TeamBalance`) instead of
always Amber, and players joining a running Random match are handed to
`RandomModeService.AddJoiner`.

Stage 13d: every connect and disconnect ends with
`GameLoop/AutoStartService.Check`, so the 2nd human starts the match and
dropping below 2 ends it.

Stage 13f: a reserved 13th connection for admins (`AdminSeat`). The server
allows 13 connections and shows 12; a non-admin is refused once 12
participants play. A seated admin is a spectator outside both teams and is
left out of every player list the game uses (`Participants`). Every admin
is seated on connect, and `dw_seat_spec` works at any time (also mid-round).
Sitting moves the admin to the spectator team and calls `MakeObserver` on
the next tick: `ChangeTeam(1)` alone leaves the hero pawn alive and the
client drops seconds later.

Stage 13g: `HeroLock` (the kill-on-hero-swap guard) moved here from Random
mode so Random and 1v1 mode each own one.

Stage 13h-13j: "the draft area" is now the watch spot (`Round/WatchSpot`):
admitting and respawning send players there restrained. Disconnects also
drop the player's restraint and 1v1 queue place; sitting in the admin seat
releases the restraint.

Join access: `bublock/access.json` on the server (next to `bublock/logs/`)
holds banned and whitelisted (`allowed`) Steam64 IDs and the open / private
mode. `OnClientConnect` checks it before the seat rule. A ban always wins,
even over admins. Private mode lets in only whitelisted IDs and `AdminAuth`
admins. Admin commands on `AccessPlugin` rewrite the file; a hand edit
applies on the next connection (the file is re-read when its write time
changes).

## Files

| File | Role |
|---|---|
| `LobbyService.cs` | Core ops (static): `ApplyServerConvars`, `AdmitPlayer`, `RemovePlayer`, `KickPlayer`, `LogDeath`, `DescribePlayer`, `ListPlayers`, `SetTeam` |
| `LobbyPlugin.cs` | Hooks and commands (thin wrappers) |
| `RiftRouletteTeams.cs` | Team numbers and names (Amber 2, Sapphire 3) |
| `TeamBalance.cs` | `Even` (fewest moves to even teams) and `SmallerTeam` (pure, tested) |
| `CommandList.cs` | Player command list for `/commands` (reflects `[Command]` attributes) |
| `Participants.cs` | Who plays: connected players minus bots and seated admins |
| `AdminSeatRule.cs` | Pure seat rules: cap 12, `CanConnect`, `CanStand`, `SeatOnJoin` (every admin; tested) |
| `AdminSeat.cs` | Admin seat service: connect gate, `Sit`, `Stand`, `Forget`, `Describe` |
| `HeroLock.cs` | Reusable hero lock (applied / pending / enforcement kills, `Enforce`) |
| `AccessRule.cs` | Pure access rule: ban, private, whitelist, admin; Steam64 ID check (tested) |
| `AccessList.cs` | `access.json` model: mode + banned / allowed sets, JSON parse and write (tested) |
| `AccessService.cs` | Access file load / save, connect gate, list changes, kick players without access |
| `AccessPlugin.cs` | Admin access commands (`/player_ban`, `/ban_*`, `/allow_*`, `/access_mode`) |

## Public operations

See `LobbyService.md`. Player commands: `/status`, `/commands` (Stage 12,
built by `CommandList`). Admin commands: `/player_list`, `/player_info`,
`/player_kick`, `/player_team`, `/lobby_setup`, `dw_seat_spec` (console
only, any time), `/seat_play`, `/seat_status`.
Access (admin): `/player_ban <slot>`, `/ban_add`, `/ban_remove`,
`/ban_list`, `/allow_add`, `/allow_remove`, `/allow_list`,
`/access_mode [open|private]` (see `AccessPlugin.md`).
Archive names `/state`,
`/kick`, `/test` were removed in Stage 12. Catalogued in
`reference/user-commands.md` and `reference/admin-commands.md`.

## State

`AdminSeat` holds the seated Steam IDs; each `HeroLock` instance is owned by
its mode. `AccessService` caches the last good `access.json` (the file is
the source of truth and survives reloads and deploys). Otherwise reads and releases picks in `Draft/DraftState` and
redraws boards with `Draft/DraftService.RedrawBoards`.

## Dependencies

- `Round/WatchSpot` (send players up), `Modules/Restraint`
  (`Forget` / `Release`), `Duel/DuelService.Forget`.
- `Draft/DraftState` and `Draft/DraftService` (board redraw).
- `GameLoop/MatchService` / `MatchConfig` and `RandomMode/RandomModeService`
  (joiners), `Stats/StatsService` (board refresh on connect / disconnect),
  `GameLoop/AutoStartService` (auto-start / auto-end on connect / disconnect).
- `Shared` (`AdminCommand`, `PlayerChat`, logging).

## Logs

- `lobby-YYYYMMDD.log`: setup, connect, disconnect, kick, team changes,
  admin command gate.
- `players-YYYYMMDD.log`: deaths (Debug), status lines.
- `access-YYYYMMDD.log`: file loads, list and mode changes, refused
  connections (Warning, so also in master), access kicks.

## Lifecycle vs commands

- Hooks run in Clean mode (lifecycle).
- Admin commands run the same ops in Debug mode.
- `/status` and `/commands` are player commands (Clean).
