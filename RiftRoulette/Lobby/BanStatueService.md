# BanStatueService

Banned players are turned to stone before they are kicked, and repeat
rejoins are refused with escalating lockouts. Static.

- Banned live (`/ban_add`, `/player_ban` while connected): statue, kicked
  after `LiveBanKickSeconds` (30).
- Banned player reconnects: let in as a statue, kicked after
  `RejoinKickSeconds` (10). Each such visit is a strike (`BanJoinRule`):
  refused for 10 min after the first, 30 min after the second, until
  restart after the third.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `AdmitBanned(steamId, name)` | From `AccessService.AllowConnect` for a banned ID. `BanJoinRule.Decide`: `Refuse` logs Warning `Connection refused, banned and locked out` and returns false. `Statue` records a strike, marks the ID as arriving, logs Warning `Banned player let in as a statue` with the new record | bool (let in) |
| `TakeArrival(steamId)` | `OnClientFullConnect`: true once for an ID `AdmitBanned` let in | bool |
| `Petrify(player, kickAfterSeconds, liveBan, timer, mode)` | Once per statue. Takes them out of the game like a leave: `DuelService.Forget`, draft pick released (boards redrawn), `RandomModeService.OnLeave` during a Random match, `StatsService.RefreshBoards`, `AutoStartService.Check(leaving)`. A rejoin (`liveBan` false) also gets `SelectHero(Skyrunner)` and the smaller team, as `AdmitPlayer`. Then `WatchSpot.SendUp` (restrained, up top). After `LoadoutService.SwapDelaySeconds` (1 s): the statue modifier and the chat line `You are banned. Do better.` (live ban adds `You will be kicked in 30s.`). Everyone else gets chat `<name> is banned.`. Kicks after `kickAfterSeconds` (`LobbyService.KickPlayer`, re-found by Steam ID). Info in `access` and master | — |
| `Sustain()` | Every 1 s (`LobbyPlugin` timer): each connected statue is restrained again and gets the statue modifier back if it lapsed | — |
| `Forget(steamId)` | From `LobbyService.RemovePlayer`: ends the statue and the arriving mark; strikes stay | — |
| `KickConnectedBanned(mode)` | Hot reload: kicks every connected human on the ban list (statue state and kick timers were lost) | kicked count |
| `IsStatue(steamId)` / `Count` | Statue checks | — |
| `Describe(steamId)` | `BanJoinRule.Describe` plus ` statue now` | text |

## Statue modifier

Added with `RestraintService.AddModifier(pawn, name, 35 s)` (duration
`LiveBanKickSeconds` + 5) only when the pawn is alive and does not have it.
The name comes from `access.json` `statueModifier` (`/ban_modifier`).
Unset: Warning once per statue `Statue modifier not set, restraint only`;
refused by the game: Warning once `Statue modifier refused`. Either way the
player is still restrained, protected from damage and kicked on time.

The stone look is `modifier_citadel_petrify` (Vyper's Petrifying Bola; the
game accepts that class name). Without Vyper in the match it showed as a red
wireframe, so `LobbyPlugin.OnPrecacheResources` precaches
`StatueLookHero` (`Heroes.Viper`) at every map load. A hot reload does not
precache; the next map load does.

## State

`Records` (Steam ID to `BanRecord`), `Statues`, `Arriving`,
`ModifierWarned`. In memory only: a restart or hot reload clears strikes.

## Invariants

- Statues are not participants (`Participants.IsParticipant`), so they get
  no team slot in round moves, no hero or build, no stats or betting row,
  and do not count for auto-start.
- A live ban is not a strike; only an allowed reconnect is.
- Never kick at connect: the kick runs from a timer after full connect.
