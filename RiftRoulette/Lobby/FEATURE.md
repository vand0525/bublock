# Lobby — Feature

## Purpose

Everything about players being on the server outside the draft and rift
logic: server convars at startup, admitting players to the draft area on
connect, returning them to draft on spawn, cleaning up on disconnect, kick,
team moves, and player status.

New connections go to the smaller team (`TeamBalance`), and players
joining a running Random match are handed to
`RandomModeService.AddJoiner`.

Every connect and disconnect ends with
`GameLoop/AutoStartService.Check`, so the 2nd human starts the match and
dropping below 2 ends it.

A reserved 13th connection for admins (`AdminSeat`). The server
allows 13 connections and shows 12; a non-admin is refused once 12
participants play. A seated admin is a spectator outside both teams and is
left out of every player list the game uses (`Participants`). Every admin
is seated on connect, and `dw_seat_spec` works at any time (also mid-round).
Sitting moves the admin to the spectator team and calls `MakeObserver` on
the next tick: `ChangeTeam(1)` alone leaves the hero pawn alive and the
client drops seconds later.

`HeroLock` (the kill-on-hero-swap guard) is shared: Random and 1v1 mode
each own one.

"The draft area" is the watch spot (`Round/WatchSpot`):
admitting and respawning send players there restrained. Disconnects also
drop the player's restraint and 1v1 queue place; sitting in the admin seat
releases the restraint.

Disconnect removes the hero pawn, the current pawn (a seated admin's
`observer` pawn) and the controller, and 1 s later removes any `observer`
pawn no connected player owns (also after a disconnect with no
controller). A pawn left behind spammed `Couldn't resolve offset ... in
CCitadelPlayerPawn` on an empty server.

Stream camera: while an admin is seated as an observer, `StreamCam` runs
their camera for streaming with no command needed. It follows a live
player's view (in-eye) and cuts to the killer when the watched player
dies. When nobody plays it parks at the framing for the current watch spot
side (default straight down over the watch spot; only moves the client in
fly cam, C). Moving the camera after that park and letting go saves the
new framing (`StreamFramingStore`, `bublock/streamcam.json`), mirrored to
the other side. It never moves the camera while the admin moves it, and
never follows a hero spawned less than 5 s ago.
Camera calls go through `Modules/Spectate`. A hot reload re-seats every
connected admin (`AdminSeat.Restore`).

Admin roaming: while no participants are connected, the seated admin plays
an invisible Abrams (`modifier_invis`, 3600 s, put back when it runs out
and on respawn) at their watch-spot slot in front of the boards, not
restrained. 2 s after a participant joins they switch to spectating (the
stream camera); 2 s after the last participant leaves (or turns to stone)
they roam again. Roaming admins stay seated, so they are never
participants.

Join access: `bublock/access.json` on the server (next to `bublock/logs/`)
holds banned and whitelisted (`allowed`) Steam64 IDs and the open / private
mode. `OnClientConnect` checks it before the seat rule. A ban always wins,
even over admins. Private mode lets in only whitelisted IDs and `AdminAuth`
admins. Admin commands on `AccessPlugin` rewrite the file; a hand edit
applies on the next connection (the file is re-read when its write time
changes).

Banned-player statue (`BanStatueService`): a ban never kicks on the spot.
A player banned while connected turns to stone up top (restrained, no
damage, out of the game like a leaver) and is kicked 30 s later. A banned
player who reconnects is let in as a statue, told `You are banned. Do
better.` and kicked 10 s later. Each such visit is a strike
(`BanJoinRule`): the next reconnect is refused for 10 min, then 30 min,
then until restart. The stone look is a game modifier named in
`access.json` `statueModifier` (`/ban_modifier`); until it is set,
statues are restrained only. `DevTools/ModifierProbe` logs modifier names
to find it (have someone cast Vyper's Petrify, then read
`modifiers-*.log`).

Pausing follows join access (`PauseGuard.FollowAccess`): on in private mode
(an organised event), off when open, set on every load and by
`/access_mode`. While off, the game's pause convars are set to 0 with the
other server convars, pause console commands are blocked in
`OnClientConCommand`, the two pause net messages are blocked with
`NetMessages.HookIncoming`, and a game that is paused anyway is unpaused
with the server `pause` toggle. `/pause_allow on|off` overrides it until
the next load or mode change.

Flex slots (`FlexSlots`): every flex item slot is open for both teams, so a
hero holds 12 items (9 by default). The team entities
(`citadel_team_manager`) get `CCitadelTeam.m_nFlexSlotsUnlocked = 15` on
startup, hot reload, every join and every intermission;
`citadel_hero_demo_unlock_flex_slots 1` alone lets the server hold 12 items but the HUD keeps drawing 9; the team field fixes the display from the next hero rebuild.

## Files

| File | Role |
|---|---|
| `LobbyService.cs` | Core ops (static): `ApplyServerConvars`, `AdmitPlayer`, `RemovePlayer`, `KickPlayer`, `LogDeath`, `DescribePlayer`, `ListPlayers`, `SetTeam` |
| `LobbyPlugin.cs` | Hooks and commands (thin wrappers) |
| `RiftRouletteTeams.cs` | Team numbers and names (Amber 2, Sapphire 3) |
| `TeamBalance.cs` | `Even` (fewest moves to even teams) and `SmallerTeam` (pure, tested) |
| `CommandList.cs` | Player command list for `/commands` (reflects `[Command]` attributes) |
| `Participants.cs` | Who plays: connected players minus bots, seated admins and statues |
| `AdminSeatRule.cs` | Pure seat rules: cap 12, `CanConnect`, `CanStand`, `SeatOnJoin` (every admin; tested) |
| `AdminSeat.cs` | Admin seat service: connect gate, `Sit`, `Stand`, `Forget`, `Restore` (after hot reload), `Describe` |
| `StreamCam.cs` | Automatic stream camera for seated admins: follow, killer cut, park at the saved framing, save the admin's framing |
| `StreamFraming.cs` | Pure framing math: pose relative to a watch spot anchor, default, pick per side, JSON (tested) |
| `StreamFramingStore.cs` | Saved framing per side in `bublock/streamcam.json` |
| `HeroLock.cs` | Reusable hero lock (applied / pending / enforcement kills, `Enforce`) |
| `AccessRule.cs` | Pure access rule: ban, private, whitelist, admin; Steam64 ID check (tested) |
| `AccessList.cs` | `access.json` model: mode + banned / allowed sets + statue modifier, JSON parse and write (tested) |
| `AccessService.cs` | Access file load / save, connect gate, list changes, statue banned players, kick private-mode outsiders |
| `AccessPlugin.cs` | Admin access commands (`/player_ban`, `/ban_*`, `/ban_modifier`, `/allow_*`, `/access_mode`) |
| `BanJoinRule.cs` | Pure rejoin rule: statue visit or refuse; strikes lock out 10 min, 30 min, then until restart (tested) |
| `BanStatueService.cs` | Statue: out of the game, up top, modifier, chat, timed kick; rejoin strikes; `Sustain` |
| `PauseRule.cs` | Pure pause rules: pause commands, pause convars, unpause and chat throttles (tested) |
| `PauseGuard.cs` | Pause service: convars, blocking pause requests, automatic unpause, `/pause_allow` state |
| `FlexSlots.cs` | Opens every flex slot on both team entities (`m_nFlexSlotsUnlocked = 15`), `Describe` |

## Public operations

See `LobbyService.md`. Player commands: `/status`, `/commands` (built by
`CommandList`). Admin commands: `/player_list`, `/player_info`,
`/player_kick`, `/player_team`, `/lobby_setup`, `/lobby_flex`, `/pause_allow`, `dw_seat_spec` (console
only, any time), `/seat_play`, `/seat_roam`, `/seat_status`, `/spec_auto`,
`/spec_status`, `/spec_reset` (stream camera).
Access (admin): `/player_ban <slot>`, `/ban_add`, `/ban_remove`,
`/ban_list`, `/ban_modifier`, `/allow_add`, `/allow_remove`, `/allow_list`,
`/access_mode [open|private]` (see `AccessPlugin.md`). Catalogued in
`reference/user-commands.md` and `reference/admin-commands.md`.

## State

`AdminSeat` holds the seated Steam IDs; `StreamCam` holds each seated
admin's camera state (auto flag, followed player, pending killer, parked
side, placed / adjusting, last pose); `StreamFramingStore` holds the saved
framing (file `bublock/streamcam.json`, survives reloads); each `HeroLock` instance is owned by
its mode. `AccessService` caches the last good `access.json` (the file is
the source of truth and survives reloads and deploys). `BanStatueService`
holds statues and rejoin strikes in memory (cleared by restart or reload). `PauseGuard` holds
the pause on / off flag (off after every load), blocked and unpause counts,
and when each player was last told pausing is off. Otherwise reads and releases picks in `Draft/DraftState` and
redraws boards with `Draft/DraftService.RedrawBoards`.

## Dependencies

- `Modules/Spectate` (stream camera calls), `Rift/RiftService`
  (`IsRunning`, `RoundNumber` for the once-per-round top-down).
- `Round/WatchSpot` (send players up; top-down camera position), `Modules/Restraint`
  (`Forget` / `Release`), `Duel/DuelService.Forget`.
- `Draft/DraftState` and `Draft/DraftService` (board redraw).
- `GameLoop/MatchService` / `MatchConfig` and `RandomMode/RandomModeService`
  (joiners), `Stats/StatsService` (board refresh on connect / disconnect),
  `GameLoop/AutoStartService` (auto-start / auto-end on connect / disconnect).
- `Shared` (`AdminCommand`, `PlayerChat`, logging).

## Logs

- `lobby-YYYYMMDD.log`: setup, flex slot unlocks, connect, disconnect, kick, team changes,
  admin command gate, stream camera moves (`Stream camera Reason=`), first
  sighting of each ability name.
- `spectate-YYYYMMDD.log`: `Modules/Spectate` follow / park details
  (Debug) and the client-command fallback (Information).
- `players-YYYYMMDD.log`: deaths (Debug), status lines.
- `access-YYYYMMDD.log`: file loads, list and mode changes, refused
  connections and statue visits (Warning, so also in master), statues,
  access kicks.
- `pause-YYYYMMDD.log`: pause convars applied, each blocked pause request
  (player, `Source=command|message`, `Detail=`), automatic unpause attempts
  (Warning, so also in master).

## Lifecycle vs commands

- Hooks run in Clean mode (lifecycle).
- Admin commands run the same ops in Debug mode.
- `/status` and `/commands` are player commands (Clean).
