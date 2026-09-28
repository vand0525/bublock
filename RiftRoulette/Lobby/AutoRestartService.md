# AutoRestartService

Watches joins and reloads the map (`Server.ChangeLevel(Server.MapName)`)
when a join got stuck or the map has been up 3 hours, only with nobody
playing (`AutoRestartRule`). A map reload keeps every client: each gets a
map-change disconnect and reconnects to the new map by itself (seated
admins included), unlike a process restart.

## State

- `Pending`: per slot, a connection let in by `OnClientConnect` that has
  not reached `OnClientFullConnect` (Steam ID, name, since, stuck flag).
- `StuckJoins`: joins that never completed since the map started (left
  before the full connect, or still connecting after 3 min).
- `Enabled`: on after every load; `/restart_auto` changes it.
- Map start time (`OnMapStart`) and last reload asked for (`Restart`).
  `UptimeSeconds` counts from the later of the two; after a hot reload
  (both unknown) it uses `GlobalVars.CurTime`.

## Operations

| Op | Behavior |
|---|---|
| `OnConnect(args, allowed)` | Allowed connections only: adds to `Pending`, Information `Client connecting Name= SteamId= Slot= MapChangeReconnect=` |
| `OnFullConnect(slot, player)` | Removes the slot from `Pending`, Information `Join completed Seconds=` |
| `OnDisconnect(slot, mapChange)` | A pending slot is removed. Unless it is a map-change disconnect: `StuckJoins++` (once per join) and Warning `Join never completed, client left ... Seconds= StuckJoins=` (copied to master) |
| `OnMapStart()` | From `LobbyPlugin.OnStartupServer`: Information `Map started, join watch reset`, clears `Pending` and `StuckJoins`, records the map start |
| `Check(mode)` | Every `CheckSeconds` (60 s, `LobbyPlugin` timer). Marks pending joins older than 3 min as stuck (Warning `Join stuck, client still connecting`, `StuckJoins++`), then `AutoRestartRule.Reason(Enabled, Participants.Humans().Count, StuckJoins, Pending.Count, UptimeSeconds)`; a reason calls `Restart` |
| `Restart(reason, mode)` | Refuses while `Server.IsChangingLevel` or with no map name (Warning). Otherwise Warning `Reloading the map Reason= Map= UptimeMinutes= StuckJoins= Pending=` (master too), records the reload time, zeroes `StuckJoins`, `Server.ChangeLevel(map)`. Returns reply text |
| `SetEnabled(enabled, mode)` | Sets `Enabled` (until the next load), Information and a master line |
| `Describe()` | On / off, map, uptime, stuck and in-progress joins, one line per pending join |

## Side effects

- A reload clears every entity and runs `OnStartupServer` in every plugin
  (convars, CleanSlate, boards, `AdminSeat.ResetForMap`). Plugin statics
  survive (no new log session).
- Seated admins reconnect and are seated again (`LobbyPlugin` forgets the
  old seat on a map-change reconnect), then roam since nobody plays.

## Invariants

- Never reloads with a participant connected (seated admins and statues
  don't count), and never twice within 10 minutes.
- Logs go to `restart-YYYYMMDD.log`.

## Dangerous Deadworks constraints

- Untested in game: that `OnStartupServer` runs on a `changelevel`, that
  a reload clears the stuck-join state (only process restarts have been
  seen to), and that `GlobalVars.CurTime` restarts with the map. The
  reload time recorded in `Restart` guards the 10 minute rule either way.
- `Server.ChangeLevel` gives up after 5 s if the map change does not start
  (Deadworks console line `changelevel didn't start within 5000 ms`).
