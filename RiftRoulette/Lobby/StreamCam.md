# StreamCam

Automatic stream camera for a seated admin (observer pawn). Runs by itself
as soon as the admin is seated: no command needed. Camera calls go through
`Modules/Spectate`; this file decides whom to watch and when. It steps
aside while the admin moves the camera by hand (manual hold). Once the
admin presses C (fly cam), it parks the camera top-down once and leaves it
there.

## State (per admin Steam ID, static)

- `Auto` (default on), `LastFollowed` (Steam ID the camera was put on),
  `PendingKiller`, `Parked` / `ParkedSide`, `OverviewEnd`, `ReturnTo`
  (player to show after the top-down), `LastShownRound`, `LastParkAt`
  (last park sent), `SeatedAt` (set by `Seated`), `ManualUntil` /
  `ManualPosition` (manual hold end and the observer position last seen
  during it).
- Fly cam: `ParkConfirmed` (the fly cam park was seen holding),
  `FlyCamSince` (first update in fly cam, restarted when a park did not
  hold), `FlyCamPosition` (observer position at the last fly cam update).
  Cleared on every update outside fly cam, by a manual hold, `SetAuto` and
  a top-down.
- `SpawnedAt` (per player Steam ID): last `player_spawn`, from `NoteSpawn`.
- `Forget(steamId)` resets everything but `Auto` and drops the spawn time
  (stand up, disconnect).
- A hot reload clears it (static); `AdminSeat.Restore` re-seats the admin
  and the next tick starts over.

## Operations

| Operation | Effect |
|---|---|
| `Tick(timer, mode)` | Every `TickSeconds` (2 s) from `LobbyPlugin.OnLoad` (with the plugin's `Timer`): for every seated admin who is observing with `Auto` on, past `SeatGrace` (3 s after `Seated`) and not in a manual hold, runs the update below. |
| `Seated(steamId)` | Called by `AdminSeat.BecomeObserver` right after `MakeObserver`; starts the seat grace, so the first park waits for the client. |
| `NoteSpawn(steamId)` | From the `player_spawn` handler (joins and respawns of participants and statues): records the spawn time for the follow delay. |
| `OnAdminCommand(admin, command, args, mode)` | From `LobbyPlugin.OnClientConCommand` for every client console command. Seated admins only: logs Information `Admin client command Command= Args=` in `spectate-*.log`. A `spec_*` command starts a manual hold (`Reason=command`), unless the seat grace is running. |
| `OnDeath(victim, attacker, timer, mode)` | If an admin's camera is on the victim (or the victim is the top-down return-to player), stores the attacker as `PendingKiller` (none for a suicide or a non-player) and runs `Tick` on the next tick. |
| `OnBigUlt(caster, ability, timer, mode)` | Skipped during a manual hold, and in fly cam (logs Information `Big ult skipped, fly cam parked Caster= Ability=`). While a top-down view shows: the caster becomes `ReturnTo` (logged, not extended). Otherwise, if `OverviewRule.CanStart(RiftService.IsRunning, RiftService.RoundNumber, LastShownRound)`: records the round and starts the top-down view with the caster as `ReturnTo` (logged `Reason=ult Caster= Ability= Round=`). Else logs Information `Big ult skipped ...`. |
| `ShowOverview(admin, timer, mode)` | `spec_overview`: starts the top-down view now, returning to the current player; does not touch `LastShownRound`. Throws `CommandException` when not observing. |
| `SetAuto(admin, on, mode)` | Sets `Auto`, clears any top-down, the park flag, the fly cam state and a manual hold. |
| `Describe(admin)` | Three status lines for `spec_status` (the first has `Manual=until HH:mm:ss UTC` or `off`, and `FlyCam=parked|settling|off`). |

## Fly cam (C pressed)

Fly cam is the observer in `Roaming` mode with no observer target. The
server cannot put the client there; only the admin pressing C does, and
the observer's mode follows the client (a server-side `Roaming` does not
stick in the directed view). Each update in fly cam runs
`SpectateRule.FlyCamStep` and nothing else (no follow, no killer cut):

- First update in fly cam: records the time and position (`Wait`). A
  `Parked` claim from the directed view is dropped.
- Still (under 50 units since the last update) for `FlyCamSettle` (1.5 s,
  so the next 2 s tick): parks over `WatchSpot.Side` (`Reason=flycam
  Side= Mode=`).
- Moving before the park: manual hold (`Reason=moved`).
- The update after a park (at least 1 s after it was sent, so the delayed
  teleport has run): at the spot (`SpectateService.IsParkedAt`) confirms
  it (logs `Stream camera fly cam parked, staying Side=`); not there
  restarts the settle.
- Confirmed: left alone. The only camera call is a repark when
  `WatchSpot.Side` changes (`Reason=flycam-side`), confirmed again the
  same way. Moving it, or it no longer at the spot: manual hold
  (`Reason=moved`); after the hold ends and the camera is still, it
  parks again.
- Leaving fly cam (C again, a click on a player) clears the fly cam
  state; the normal update follows or parks from there.

## Manual hold

Moving the camera by hand while the server also drives it has crashed the
client (2026-09-28), so the camera never fights the admin:

- Started by: a `spec_*` console command (`Reason=command`); moving in fly
  cam before a park, or moving a confirmed fly cam park (`Reason=moved`).
  Pressing C sends no console command.
- Starting it clears top-down, return-to, pending killer, the followed
  player, the park flag and the fly cam state. Logs Information `Stream
  camera paused, manual control Reason= HoldSeconds=` once per hold.
- Lasts `ManualHold` (60 s). Every tick where the observer moved more than
  50 units since the last check extends it, so it ends 60 s after the
  camera stops moving. Then logs `Stream camera resumed after manual
  control` and the next update follows or parks afresh.
- During it: no follow, park, repark, killer cut or top-down.

## Update (one admin)

1. Top-down showing: do nothing. Just ended: remember that it ended.
2. In fly cam: the fly cam step above, stop. Otherwise clear the fly cam
   state.
3. Candidates: participants (`Participants.Humans()`) with a live hero
   pawn that spawned at least `FollowGrace` (5 s) ago (a pawn only seconds
   old preceded a client crash), shuffled. If any of them are not
   restrained (fighting), only those.
4. Just ended and `ReturnTo` is a candidate: follow them (`Reason=caster`).
5. Otherwise `SpectateRule.Choose(current, PendingKiller, candidates)`,
   where current is the candidate the observer is on, else `LastFollowed`
   if still a candidate (the server target did not stick; it is kept
   rather than switching every tick), and null right after a top-down.
   - `Keep`: stay; a manual click to another live player is adopted and
     logged once (`Reason=keep`).
   - `Killer` / `Any`: `SpectateService.Follow` (`Reason=killer|any`,
     `Accepted=`).
   - `Park`: top-down over `WatchSpot.Side`, sent when parking starts or
     the side changes (`Reason=park Side= Mode=`). Outside fly cam the
     park does not move the client, so while parked on the same side it
     is sent again every `ReparkEvery` (6 s) (`Reason=repark`), ready for
     the moment the admin presses C.

## Top-down view

- Position: the main watch spot anchor (`WatchSpot.Location(side)`, not a
  per-slot spot), above the rift being fought or the next one, raised by
  `OverheadHeight` (264, so z 1800 over the 1536 floor).
- Sent through `SpectateService.Park`: teleport, then the angle. Only
  moves the view when the admin is in fly cam (C).
- Angle: `SpectateRule.LookDown(anchor yaw)`: pitch 89, the watch spot's
  yaw.
- Length: `OverviewRule.Duration` (10 s); a `timer.Once` runs `Tick` when
  it ends, so the cut back is on time.

## Invariants

- Never touches players, only the admin's own camera.
- At most one ult-triggered top-down per live round; none in fly cam.
- In fly cam the camera is parked once and then only moved when the watch
  spot changes side.
- Logs every camera move in `lobby-*.log` as `Stream camera Reason=`
  (`keep`, `killer`, `any`, `ult`, `caster`, `park`, `repark`, `overview`,
  `flycam`, `flycam-side`), plus the fly cam confirmation and the manual
  hold start and end.

## Dangerous Deadworks constraints

- The client starts in the directed view; the camera only moves once the
  admin presses C for fly cam. The server cannot switch it: the client
  refuses `spec_mode` / `spec_player` from the server (not
  `server_can_execute`), so the camera sends no client commands.
- `Parked` is only a claim: the client can leave fly cam (C key, or the
  directed view taking over), so a fly cam park is confirmed by position
  before it counts.
- Never move the camera while the admin is moving it (manual hold): the
  client crashed when both did.
- Whether pitch 89 holds after the delayed angle is still to confirm in
  game (`Parked ... Mode=` in `spectate-*.log` at Debug).
