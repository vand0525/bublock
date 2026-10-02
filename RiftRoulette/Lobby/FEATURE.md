# Lobby — Feature

## Purpose

Everything about players being on the server outside the hero modes and
rift logic: server convars at startup, admitting players up top on
connect, sending them back up on spawn, keeping them on the right hero
(`LobbyHeroes`), cleaning up on disconnect, kick, team moves, and player
status.

New connections go to the smaller team (`TeamBalance`), and players
joining a running Random match are handed to
`RandomModeService.AddJoiner` (a running Mirror match:
`MirrorModeService.AddJoiner`).

Every connect and disconnect ends with
`GameLoop/AutoStartService.Check`, so the 2nd human starts the match and
dropping below 2 ends it.

A reserved 13th connection for admins (`AdminSeat`). The server
allows 13 connections and shows 12; a non-admin is refused once 12
participants play. A seated admin is a spectator outside both teams and is
left out of every player list the game uses (`Participants`). An admin
rejoins (map change or reconnect) in their last mode: spectating,
roaming or playing (`AdminSeat.LastMode`); with none they play, and the
13th connection (12 others playing) never plays. `dw_seat_spec` works at
any time (also mid-round).
Sitting moves the admin to the spectator team and calls `MakeObserver` on
the next tick: `ChangeTeam(1)` alone leaves the hero pawn alive and the
client drops seconds later.

`HeroLock` (the kill-on-hero-swap guard) is shared: Random and Mirror mode
each own one. `LobbyHeroes` runs on every `player_hero_changed`: the
mode's hero guard first, else the player is held to their round hero
(`Round/RoundHeroes`) or the lobby hero (Abrams). At match end and on a
mode change `LobbyHeroes.ReturnAll` puts everyone back on the lobby hero
up top.

"Up top" is the watch spot (`Round/WatchSpot`):
admitting and respawning send players there restrained. Disconnects also
drop the player's restraint and round hero; sitting in the admin seat
releases the restraint.

Disconnect removes the hero pawn, the current pawn (a seated admin's
`observer` pawn) and the controller, and 1 s later removes any `observer`
pawn no connected player owns (also after a disconnect with no
controller). A pawn left behind spammed `Couldn't resolve offset ... in
CCitadelPlayerPawn` on an empty server.

Stream camera: while an admin is seated as an observer, `StreamCam` runs
their camera for streaming with no command needed. It follows a live
fighter's view (in-eye) and cuts to the killer when the watched player
dies. When nobody fights (between rounds, everyone up top is restrained)
it parks at the fixed spot for the current watch spot side
(`StreamFraming.Spot`, captured in fly cam). The server cannot switch the
client into fly cam (C), so outside fly cam it re-sends the park every
6 s and pressing C lands on the spot. It never moves the camera while the
admin moves it (a camera moved after it landed stays until the side
changes), and never follows a hero spawned less than 5 s ago.
Camera calls go through `Modules/Spectate`. A hot reload keeps each
admin's mode, read back from the pawn (`AdminSeat.Restore`).

Admin roaming: manual only (`/seat_roam`, from spectating or playing; a
playing admin leaves their team first). The admin becomes an
invisible Abrams (`modifier_invis`, 3600 s, put back when it runs out
and on respawn) in front of the welcome sign, not restrained, and may
roam while players are connected (game chat). When the watch spot moves
to another rift side, a live roaming Abrams is moved to the welcome sign
there within 2 s (`AdminSeat.FollowWatchSpot`). `dw_seat_spec` returns to
spectating. There is no auto switch on join/leave. Roaming admins stay
seated, so they are never participants.

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
| `LobbyHeroes.cs` | Hero enforcement on `player_hero_changed` (`Enforce`) and everyone back to the lobby hero (`ReturnAll`) |
| `RiftRouletteTeams.cs` | Team numbers and names (Amber 2, Sapphire 3) |
| `TeamBalance.cs` | `Even` (fewest moves to even teams) and `SmallerTeam` (pure, tested) |
| `CommandList.cs` | Player command list for `/commands` (reflects `[Command]` attributes) |
| `AboutText.cs` | Chat lines for `/about`: the mode and betting (pure, tested) |
| `Participants.cs` | Who plays: connected players minus bots, players whose disconnect is being handled, seated admins and statues |
| `AdminSeatRule.cs` | Pure seat rules: cap 12, `CanConnect`, `CanStand`, `AdminMode`, `JoinMode` (last mode, never play as the 13th; tested) |
| `AdminSeat.cs` | Admin seat service: connect gate, `Sit`, `Stand`, `Forget`, `Restore` (after hot reload), roam follows the watch spot, `Describe` |
| `StreamCam.cs` | Automatic stream camera for seated admins: follow fighters, killer cut, park at the fixed spot between rounds |
| `StreamCamRule.cs` | Pure park rule: park, repark or stay (tested) |
| `StreamFraming.cs` | Fixed camera spot (position and angle) per rift side (tested) |
| `HeroLock.cs` | Reusable hero lock (applied / pending / enforcement kills, `Enforce`) |
| `AccessRule.cs` | Pure access rule: ban, private, whitelist, admin; Steam64 ID check (tested) |
| `AccessList.cs` | `access.json` model: mode + banned / allowed sets + statue modifier, JSON parse and write (tested) |
| `AccessService.cs` | Access file load / save, connect gate, list changes, statue banned players, kick private-mode outsiders |
| `AccessPlugin.cs` | Admin access commands (`/player_ban`, `/ban_*`, `/ban_modifier`, `/allow_*`, `/access_mode`) |
| `BanJoinRule.cs` | Pure rejoin rule: statue visit or refuse; strikes lock out 10 min, 30 min, then until restart (tested) |
| `OrphanObserverRule.cs` | Pure disconnect sweep rule: only the leaver's `observer` pawns are removed, never another slot's (tested) |
| `BanStatueService.cs` | Statue: out of the game, up top, modifier, chat, timed kick; rejoin strikes; `Sustain` |
| `PauseRule.cs` | Pure pause rules: pause commands, pause convars, unpause and chat throttles (tested) |
| `PauseGuard.cs` | Pause service: convars, blocking pause requests, automatic unpause, `/pause_allow` state |
| `FlexSlots.cs` | Opens every flex slot on both team entities (`m_nFlexSlotsUnlocked = 15`), `Describe` |
| `AutoRestartRule.cs` | Pure map reload rule: stuck join or 3 h up, only with nobody playing, not within 10 min of a map start (tested) |
| `AutoRestartService.cs` | Join watch (connect to full connect), stuck joins, the 60 s check and `Server.ChangeLevel` map reload |
| `MapRefreshRule.cs` | Pure join budget rule: due at 160 fighter-rounds, rounds left (tested) |
| `MapRefreshService.cs` | Join budget: counts fighter-rounds, at the budget warns in chat, ends the match at a round end and reloads the map |

## Public operations

See `LobbyService.md`. Player commands: `/status`, `/commands` (built by
`CommandList`), `/about` (`AboutText`). Admin commands: `/player_list`, `/player_info`,
`/player_kick`, `/player_team`, `/lobby_setup`, `/lobby_flex`, `/pause_allow`, `dw_seat_spec` (console
only, any time), `/seat_play`, `/seat_roam`, `/seat_status`, `/spec_auto`,
`/spec_status` (stream camera), `/restart_status`,
`/restart_now`, `/restart_auto` (automatic map reload), `/restart_budget`
(join budget refresh).
Access (admin): `/player_ban <slot>`, `/ban_add`, `/ban_remove`,
`/ban_list`, `/ban_modifier`, `/allow_add`, `/allow_remove`, `/allow_list`,
`/access_mode [open|private]` (see `AccessPlugin.md`). Catalogued in
`reference/user-commands.md` and `reference/admin-commands.md`.

## State

`AdminSeat` holds the seated Steam IDs; `StreamCam` holds each seated
admin's camera state (auto flag, followed player, pending killer, parked
side, placed / handled, last pose) and roaming admins' watch spot side;
each `HeroLock` instance is owned by
its mode. `AccessService` caches the last good `access.json` (the file is
the source of truth and survives reloads and deploys). `BanStatueService`
holds statues and rejoin strikes in memory (cleared by restart or reload). `PauseGuard` holds
the pause on / off flag (off after every load), blocked and unpause counts,
and when each player was last told pausing is off. `AutoRestartService`
holds the joins in progress, the stuck-join count (both cleared at every
map start), the on / off flag (on after every load) and the map start /
last reload times. `MapRefreshService` holds the fighter-rounds since the
map start, the budget (default 160 after every load) and the pending
reload flag. Otherwise reads and drops round heroes in `Round/RoundHeroes`.

## Dependencies

- `Modules/Spectate` (stream camera calls).
- `Round/WatchSpot` (send players up; camera spot and roam side),
  `Modules/Restraint` (`Forget` / `Release`; the camera follows only
  unrestrained players), `RandomMode/RandomModeService` and
  `Mirror/MirrorModeService` (`Forget`, `GuardHero`).
- `Round/RoundHeroes` (round heroes), `Boards/BoardService` is called by
  `MatchService` after `LobbyHeroes.ReturnAll`.
- `GameLoop/MatchService` / `MatchConfig`, `RandomMode/RandomModeService`
  and `Mirror/MirrorModeService` (joiners), `Stats/StatsService` (board refresh on connect / disconnect),
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
- `restart-YYYYMMDD.log`: every allowed connection (`Client connecting`),
  `Join completed Seconds=`, stuck joins and map reloads (Warning, so
  also in master), map starts.

## Lifecycle vs commands

- Hooks run in Clean mode (lifecycle).
- Admin commands run the same ops in Debug mode.
- `/status`, `/commands` and `/about` are player commands (Clean).
