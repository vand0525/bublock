# StreamCam

Automatic stream camera for a seated admin (observer pawn). Runs by itself
as soon as the admin is seated: no command needed. Camera calls go through
`Modules/Spectate`; this file decides whom to watch and when. It steps
aside while the admin moves the camera by hand (manual hold).

## State (per admin Steam ID, static)

- `Auto` (default on), `LastFollowed` (Steam ID the camera was put on),
  `PendingKiller`, `Parked` / `ParkedSide`, `OverviewEnd`, `ReturnTo`
  (player to show after the top-down), `LastShownRound`, `LastParkAt`
  (last park sent), `SeatedAt` (set by `Seated`), `ManualUntil` /
  `ManualPosition` (manual hold end and the observer position last seen
  during it).
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
| `OnBigUlt(caster, ability, timer, mode)` | Skipped during a manual hold. While a top-down view shows: the caster becomes `ReturnTo` (logged, not extended). Otherwise, if `OverviewRule.CanStart(RiftService.IsRunning, RiftService.RoundNumber, LastShownRound)`: records the round and starts the top-down view with the caster as `ReturnTo` (logged `Reason=ult Caster= Ability= Round=`). Else logs Information `Big ult skipped ...`. |
| `ShowOverview(admin, timer, mode)` | `spec_overview`: starts the top-down view now, returning to the current player; does not touch `LastShownRound`. Throws `CommandException` when not observing. |
| `SetAuto(admin, on, mode)` | Sets `Auto`, clears any top-down, the park flag and a manual hold. |
| `Describe(admin)` | Three status lines for `spec_status` (the first has `Manual=until HH:mm:ss UTC` or `off`). |

## Manual hold

Moving the camera by hand while the server also drives it has crashed the
client (2026-09-28), so the camera never fights the admin:

- Started by: a `spec_*` console command (`Reason=command`); flying away
  from the park spot, i.e. parked, fly cam, no target, farther than
  `SpectateService.ParkTolerance` (`Reason=moved`, instead of a repark);
  leaving a follow, i.e. the followed player is still a candidate but the
  observer is in fly cam with no target (`Reason=left-follow`).
- Starting it clears top-down, return-to, pending killer, the followed
  player and the park flag. Logs Information `Stream camera paused, manual
  control Reason= HoldSeconds=` once per hold.
- Lasts `ManualHold` (60 s). Every tick where the observer moved more than
  50 units since the last check extends it, so it ends 60 s after the
  camera stops moving. Then logs `Stream camera resumed after manual
  control` and the next update follows or parks afresh.
- During it: no follow, park, repark, killer cut or top-down.

## Update (one admin)

1. Top-down showing: do nothing. Just ended: remember that it ended.
2. Candidates: participants (`Participants.Humans()`) with a live hero
   pawn that spawned at least `FollowGrace` (5 s) ago (a pawn only seconds
   old preceded a client crash), shuffled. If any of them are not
   restrained (fighting), only those.
3. Just ended and `ReturnTo` is a candidate: follow them (`Reason=caster`).
4. The admin left a follow (see Manual hold): start the hold, stop.
5. Otherwise `SpectateRule.Choose(current, PendingKiller, candidates)`,
   where current is the candidate the observer is on, else `LastFollowed`
   if still a candidate (the server target did not stick; it is kept
   rather than switching every tick), and null right after a top-down.
   - `Keep`: stay; a manual click to another live player is adopted and
     logged once (`Reason=keep`).
   - `Killer` / `Any`: `SpectateService.Follow` (`Reason=killer|any`,
     `Accepted=`).
   - `Park`: top-down over `WatchSpot.Side`, sent when parking starts or
     the side changes (`Reason=park Side= Mode=`). While parked on the same
     side, at most every `ReparkEvery` (6 s) it checks
     `SpectateService.IsParkedAt`. Flown away in fly cam: manual hold.
     Not roaming or with a target (for example the directed view put it
     on the Patron): parks again (`Reason=repark`).

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
- At most one ult-triggered top-down per live round.
- Logs every camera move in `lobby-*.log` as `Stream camera Reason=`
  (`keep`, `killer`, `any`, `ult`, `caster`, `park`, `repark`, `overview`),
  plus the manual hold start and end.

## Dangerous Deadworks constraints

- The client starts in the directed view; the camera only moves once the
  admin presses C for fly cam. The server cannot switch it: the client
  refuses `spec_mode` / `spec_player` from the server (not
  `server_can_execute`), so the camera sends no client commands.
- `Parked` is only a claim: the client can leave fly cam (C key, or the
  directed view taking over), so the park is re-checked, not trusted.
- Never move the camera while the admin is moving it (manual hold): the
  client crashed when both did.
- Whether pitch 89 holds after the delayed angle is still to confirm in
  game (`Parked ... Mode=` in `spectate-*.log` at Debug).
- Whether the client's spectator keys arrive as `spec_*` console commands
  is to confirm from `Admin client command` lines; the movement checks
  work without them.
