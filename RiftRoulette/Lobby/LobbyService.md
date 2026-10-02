# LobbyService

Core Lobby operations: server setup, admitting and removing players, kick,
team changes, and player descriptions. Static; no plugin-class dependency.
`AdmitPlayer` and `RemovePlayer` take the caller's `ITimer` (Random mode
joiner fallback, match auto-start / auto-end).

## Logs

- `Lobby` feature log (`lobby-YYYYMMDD.log`): setup, admit, disconnect,
  kick, team changes.
- `Players` feature log (`players-YYYYMMDD.log`): deaths (and the
  `/status` / `/player_info` lines logged by `LobbyPlugin`).
- Ops that change state take `ExecutionMode mode = Clean`.

## Operations

`LobbyHero` = `Heroes.Atlas` (Abrams): lobby placeholder for admit, match
end / mode change (`LobbyHeroes.ReturnAll`), statue rejoin, and admin roam
(`AdminSeat.RoamHero`). Skyrunner stopped spawning after engine 6712.

| Op | Behavior | Returns |
|---|---|---|
| `ApplyServerConvars(mode)` | In this order: `citadel_team_size 6`, `maxplayers 13` (12 players + the admin seat, `AdminSeatRule.PlayerCap + 1`), `sv_visiblemaxplayers 12`, `citadel_koth_enabled 0`, execute `citadel_koth_warning_time 1`, `citadel_koth_early_warning_time 1`, `citadel_player_override_spawn_time 1`, `citadel_allow_duplicate_heroes 1`, `citadel_hero_demo_unlock_flex_slots 1`, `citadel_voice_all_talk 1` (everyone hears everyone on voice), then `FlexSlots.UnlockAll(mode)` (every flex slot open on both teams; the convar lets the server hold 12 items, the team field makes the HUD draw them on the next hero rebuild), then `ShopAccess.Disable(mode)` (buying anywhere off), then `PauseGuard.FollowAccess(mode)` (pausing on when `access.json` is private, off when open). `ConVar.Find` convars go through `ServerConVars.TrySet`, which logs one `Convar missing` Warning per missing name. Logs Information (`TeamSize`, `MaxPlayers`, `Visible`). Also runs on a hot reload (`LobbyPlugin.OnLoad`) | — |
| `AdmitPlayer(player, timer, mode)` | Team = `TeamBalance.SmallerTeam` over the other participants' `TeamNum` (`Participants.Humans()`: no bots, no seated admin; random on a tie), so 1 on Amber puts the next player on Sapphire. Then `FlexSlots.UnlockAll(mode)`, `SelectHero(LobbyHero)` (`Heroes.Atlas` / Abrams; was Skyrunner until engine 6712 made it unselectable), `ChangeTeam(team, true)`, `WatchSpot.SendUp` (restrain + watch spot), log `Player admitted Team=`. If a Random mode match is running: `RandomModeService.AddJoiner` (a hero right away during an intermission); a Mirror mode match: `MirrorModeService.AddJoiner` (the shared hero and build). Then `StatsService.RefreshBoards`, then `AutoStartService.CheckSoon(timer, mode)` (2 s later; the 2nd human starts the match once the joiner's `SelectHero` has settled, else the match's hero swaps were lost). No bot check on the admitted player | team number |
| `RemovePlayer(player, timer, mode)` | `Participants.MarkLeaving` first (the leaver is out of every player list, so a match the leave ends never swaps or sends up the leaving controller), then `AdminSeat.Forget` (drops a seat), `BanStatueService.Forget` (ends a statue; rejoin strikes stay), `RestraintService.Forget`, `WatchGuard.Forget`, `StatsService.Forget` (drops their assist damage totals), then logs the disconnect. If the player held a round hero: `RoundHeroes.Remove`, log `Disconnected player removed Hero=`. During a Random match: `RandomModeService.OnLeave` (in an intermission the bench player takes the leaver's team); during a Mirror match: `MirrorModeService.OnLeave` (same rule). Then removes the hero pawn, the current pawn when it is a different entity (a seated admin's `observer` pawn), and the controller; logs `Disconnect pawns removed Hero= HeroIndex= Current= CurrentIndex= Removed=`. Schedules `SweepOrphanObservers` `OrphanSweepSeconds` (1 s) later. Then `StatsService.RefreshBoards`, then `AutoStartService.Check(timer, mode, steamId)` leaving out the departing player (under 2 humans ends the match; a disconnect never starts one) | — |
| `OnDisconnectWithoutController(slot, reason, timer, mode)` | Called when `OnClientDisconnect` has no controller (`reason` is `ENetworkDisconnectionReason`). Warning `Disconnect without a controller Slot= Reason=`, then `SweepOrphanObservers` 1 s later | — |
| `SweepOrphanObservers(mode)` | Removes every `observer` pawn (`SpectateService.ObserverDesignerName`) that no connected player's `Pawn` points to; Warning `Orphan observer pawn removed Index= Class=` per removal. Never touches hero pawns (they lose their controller for a moment during a rebuild) | count removed |
| `KickPlayer(slot, mode)` | Finds the slot in `Players.GetAll()`. Empty slot: Warning, returns false. Otherwise logs `Kicking player` with a `PlayerRef`, then `kickid <slot>` (the disconnect that follows runs `RemovePlayer`) | `bool` kicked |
| `LogDeath(player, pawn)` | Debug line in `Players` (life state, health, position). Takes the base controller / pawn types the `player_death` event provides. Written only when that logger allows Debug | — |
| `DescribePlayer(player)` | One line: slot, name, Steam ID, team name, `RoundHero=` (or `-`), then hero, life state, alive, health, max health, position, entity index; `Pawn=NULL` instead of the pawn fields when there is no hero | `string` |
| `ListPlayers()` | `DescribePlayer` for every fully connected player | lines |
| `SetTeam(player, team, mode)` | Refuses (logs, returns false) if the player holds a round hero (they are fighting this round; move them between rounds). Otherwise `ChangeTeam(team)` (the overload `/select` uses) | `bool` changed |

## Side effects and constraints

- A pawn left in the world after its client drops keeps queuing network
  changes the server cannot send: the console repeats `Couldn't resolve
  offset 488 in CCitadelPlayerPawn at path (4 = '26')` (and 496) on an
  empty server until the entity is removed. That is why disconnect removes
  every pawn the controller holds.
- `ChangeTeam` has a documented visual-update caveat (see
  `reference/resources.md`); confirmed in game.
- Game-thread only.
