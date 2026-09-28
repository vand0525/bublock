# LobbyService

Core Lobby operations: server setup, admitting and removing players, kick,
team changes, and player descriptions. Static; no plugin-class dependency.
`AdmitPlayer` and `RemovePlayer` take the caller's `ITimer` (Random mode
joiner fallback, match auto-start / auto-end).
Extracted from Legacy in Stage 8; balanced joins in Stage 13c; admin seat
and participants in Stage 13f.

## Logs

- `Lobby` feature log (`lobby-YYYYMMDD.log`): setup, admit, disconnect,
  kick, team changes.
- `Players` feature log (`players-YYYYMMDD.log`): deaths (and the
  `/status` / `/player_info` lines logged by `LobbyPlugin`).
- Ops that change state take `ExecutionMode mode = Clean`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `ApplyServerConvars(mode)` | Archive `OnStartupServer` block, same order: `citadel_team_size 6`, `maxplayers 13` (12 players + the admin seat, `AdminSeatRule.PlayerCap + 1`), `sv_visiblemaxplayers 12`, `citadel_koth_enabled 0`, execute `citadel_koth_warning_time 1`, `citadel_koth_early_warning_time 1`, `citadel_player_override_spawn_time 1`, `citadel_allow_duplicate_heroes 1`, then `ShopAccess.Sync(mode)` (buying anywhere on only in 1v1 setup; the archive always set it to 1), then `PauseGuard.Apply(mode)` (pause convars off unless `/pause_allow on`). `ConVar.Find` convars go through `ServerConVars.TrySet`, which logs one `Convar missing` Warning per missing name. Logs Information (`TeamSize`, `MaxPlayers`, `Visible`). Also runs on a hot reload (`LobbyPlugin.OnLoad`) | — |
| `AdmitPlayer(player, timer, mode)` | Team = `TeamBalance.SmallerTeam` over the other participants' `TeamNum` (`Participants.Humans()`: no bots, no seated admin; random on a tie), so 1 on Amber puts the next player on Sapphire. Then `SelectHero(Skyrunner)`, `ChangeTeam(team, true)`, `WatchSpot.SendUp` (restrain + watch spot), log with `Team=`. If a Random mode match is running: `RandomModeService.AddJoiner` (a hero right away during an intermission). In 1v1 mode with no match: `DuelService.GrantSetup(announce: true)` (100,000 souls and the "1v1 setup" banner). Then `StatsService.RefreshBoards`, then `AutoStartService.CheckSoon(timer, mode)` (2 s later; the 2nd human starts the match once the joiner's `SelectHero` has settled, else the match's hero swaps were lost). No bot check on the admitted player (same as the archive) | team number |
| `RemovePlayer(player, timer, mode)` | `AdminSeat.Forget` (drops a seat), `BanStatueService.Forget` (ends a statue; rejoin strikes stay), `RestraintService.Forget`, `DuelService.Forget` (leaves the 1v1 queue), then logs the disconnect. If the player had a pick: `DraftState.Release`, log `Disconnected player removed Hero=`, `DraftService.RedrawBoards(mode)`. During a Random match: `RandomModeService.OnLeave` (in an intermission the bench player takes the leaver's team). Then removes the hero pawn and the controller (archive order), then `StatsService.RefreshBoards`, then `AutoStartService.Check(timer, mode, steamId)` leaving out the departing player (under 2 humans ends the match; a disconnect never starts one) | — |
| `KickPlayer(slot, mode)` | Finds the slot in `Players.GetAll()`. Empty slot: Warning, returns false. Otherwise logs `Kicking player` with a `PlayerRef`, releases any pick and redraws boards, then `kickid <slot>` | `bool` kicked |
| `LogDeath(player, pawn)` | Debug line in `Players` (life state, health, position). Takes the base controller / pawn types the `player_death` event provides. Written only when that logger allows Debug | — |
| `DescribePlayer(player)` | One line: slot, name, Steam ID, team name, pick (or `-`), then hero, life state, alive, health, max health, position, entity index; `Pawn=NULL` instead of the pawn fields when there is no hero | `string` |
| `ListPlayers()` | `DescribePlayer` for every fully connected player | lines |
| `SetTeam(player, team, mode)` | Refuses (logs, returns false) if the player has a pick, since the pick decides the team. Otherwise `ChangeTeam(team)` (the overload `/select` uses) | `bool` changed |

## Side effects and constraints

- `RemovePlayer` and `KickPlayer` redraw the draft boards with
  `DraftService.RedrawBoards(mode)` after releasing a pick.
- `KickPlayer`'s `kickid` triggers a disconnect, which runs `RemovePlayer`
  again; the pick is already released, so only the removal happens (same as
  the archive).
- `ChangeTeam` has a documented visual-update caveat (see
  `reference/resources.md`); confirmed in game at Stage 12.
- Game-thread only.
