# PauseGuard

Keeps players from pausing the server. Static; one per DLL load. Pausing
follows join access: on every load `FollowAccess` turns it on when
`access.json` is private (an organised event) and off when open.
`/access_mode open|private` switches pausing with the mode, and
`/pause_allow on|off` overrides it until the next load or mode change.
Logs to `pause-*.log` (`BublockLog.For("Pause")`).

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Apply(mode)` | Sets `PauseRule.ConVars(Allowed)` with `ServerConVars.TrySet` (one Warning per missing convar); Info `Pause convars applied Allowed=` | — |
| `FollowAccess(mode)` | `Allowed = AccessService.Load().Private`, then `Apply` | — |
| `SetAllowed(allowed, mode)` | Stores `Allowed`, calls `Apply`, master line `Pausing turned on/off` | reply text |
| `Block(player, source, detail)` / `Block(slot, source, detail)` | When pausing is off: counts the block (`Blocked`, `EventCounters` `pause_blocked_<source>`), logs Info `Pause blocked Source= Detail=` with the player, and sends that player `OffMessage` in chat at most once per 5 s (`PauseRule.ShouldTell`). When on: does nothing | true when the pause must be stopped |
| `Tick()` | When `GameRules.GamePaused` and pausing is off, at most once per 2 s (`PauseRule.ShouldUnpause`): runs the server command `pause` (a toggle), counts `Unpauses` and `pause_auto_unpause`, logs a Warning with `ServerPaused` | — |
| `Describe()` | One line: on/off, `GamePaused`, `ServerPaused`, blocked count, automatic unpause count | lines |

`source` is `command` (client console command) or `message` (pause net
message); `detail` is the command name or message type.

## Called from

- `LobbyService.ApplyServerConvars` → `FollowAccess` (startup, hot reload, `/lobby_setup`).
- `/access_mode open` / `private` → `SetAllowed(false / true)`.
- `LobbyPlugin.OnClientConCommand` → `Block` for `PauseRule.IsPauseCommand`.
- `LobbyPlugin` net message hooks (`CCLCMsg_RequestPause`, `CCitadelClientMsg_Pause`) → `Block(slot, ...)`.
- `LobbyPlugin.OnGameFrame` → `Tick` (every frame, also while not simulating, which is the paused state).
- `/pause_allow` → `SetAllowed` / `Describe`.

## Invariants

- Chat only, never a banner.
- Admins are blocked like everyone else while pausing is off.
- Never touches `citadel_pause_count` / `citadel_num_team_pauses_allowed`
  (0 means unlimited).
- Throttles use `Environment.TickCount64`; game time stops while paused.

## Dangerous constraints

- The automatic unpause relies on the server `pause` command toggling
  `CGameRules.m_bGamePaused` back off; not yet verified in game. The Warning
  line shows each attempt, so a toggle that does not work shows up as
  repeated attempts every 2 s.
