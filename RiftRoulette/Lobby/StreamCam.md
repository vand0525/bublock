# StreamCam

Automatic stream camera for a seated admin (observer pawn). Runs by itself
as soon as the admin is seated: no command needed. Camera calls go through
`Modules/Spectate`; this file decides what to show.

- Players to watch: follow one (in-eye), cut to the killer when they die.
- Nobody to watch: park at the framing for the current watch spot side
  (`WatchSpot.Side`). The default framing is straight down, 264 units
  above the watch spot anchor. Move the camera after that park and let go:
  where it stops becomes the saved framing (`StreamFramingStore`), used
  for every later park on both sides.
- Never moves the camera while the admin is moving it.

## State (per admin Steam ID, static)

- `Auto` (default on), `SeatedAt` (set by `Seated`).
- Follow: `LastFollowed`, `PendingKiller`, `FollowSentAt`,
  `FollowFailLogged`.
- Park: `ParkedSide`, `LastParkAt`, `ParkTarget` (world position sent),
  `ParkPending` (a fly cam park not yet checked), `Placed` (the camera is
  at the framing and has not been moved since), `Adjusting` (the admin is
  moving it away from the framing).
- `LastPosition` / `LastAngles`: the observer's position and view angle at
  the last update, to see the admin moving it.
- `SpawnedAt` (per player Steam ID): last `player_spawn`, from `NoteSpawn`.
- `Forget(steamId)` resets everything but `Auto` and drops the spawn time
  (stand up, disconnect). A hot reload clears it (static);
  `AdminSeat.Restore` re-seats the admin. The saved framing is in a file,
  so it survives.

## Operations

| Operation | Effect |
|---|---|
| `Tick(timer, mode)` | Every `TickSeconds` (2 s) from `LobbyPlugin.OnLoad`: for every seated admin who is observing with `Auto` on and past `SeatGrace` (3 s after `Seated`), runs the update below. |
| `Seated(steamId)` | Called by `AdminSeat.BecomeObserver` right after `MakeObserver`; starts the seat grace. |
| `NoteSpawn(steamId)` | From the `player_spawn` handler: records the spawn time for the follow delay. |
| `OnDeath(victim, attacker, timer, mode)` | If an admin's camera follows the victim, stores the attacker as `PendingKiller` (none for a suicide or a non-player) and runs `Tick` on the next tick. |
| `SetAuto(admin, on, mode)` | Sets `Auto` and resets the rest of that admin's state. |
| `ResetFraming(mode)` | `spec_reset`: `StreamFramingStore.Reset` and every admin's `Placed` / `Adjusting` cleared, so the next update parks at the default. |
| `Describe(admin)` | Three lines for `spec_status`: auto, seat, observer mode, fly cam, the view angle read (or `unreadable`); who is watched, parked side, placed, adjusting, watch side; the saved framing per side (or `default`). |

## Update (one admin)

Fly cam is `SpectateService.IsFlyCam` (observer `Roaming`, no target; only
the admin pressing C puts the client there). "Moved" means, in fly cam,
more than 50 units or 3 degrees since the last update
(`SpectateRule.HandMoved`).

1. A fly cam park sent: wait `ParkSettle` (1.25 s) for its teleport and
   angles, then check it once: within `PlacedUnits` (100) of the target
   and still in fly cam means `Placed`. Nothing else that update.
2. Candidates: participants (`Participants.Humans()`) with a live hero
   pawn spawned at least `FollowGrace` (5 s) ago, shuffled; only the
   fighting (not restrained) ones if there are any.
3. Candidates: follow.
   - Moved: do nothing this update.
   - A follow was sent but the camera is not on that player (still a
     candidate): logs Information `Stream camera follow did not take
     Target= Mode= FlyCam=` once, and sends it again after `FollowRetry`
     (10 s, logged at Debug as `Reason=retry`).
   - Else `SpectateRule.Choose(current, PendingKiller, candidates)`:
     `Keep` (a manual click to another live player is adopted, logged once
     `Reason=keep`), `Killer` / `Any` (`SpectateService.Follow`, logged
     `Reason=killer|any Target= Accepted= Mode=`).
4. No candidates: park.
   - Not in fly cam: the park does not move the client, so it is sent when
     the side changes (`Reason=park`, Information) and again every
     `ReparkEvery` (6 s, `Reason=repark`, Debug), ready for when the admin
     presses C.
   - In fly cam: `SpectateRule.FramingStep(Placed, Adjusting, moved,
     ParkedSide != WatchSpot.Side)`:
     - `Wait` (moving before any park): nothing.
     - `Adjust` (moving the placed camera): `Adjusting`.
     - `Save` (let go after adjusting): the current position and view
       angle become the framing of the side it was parked for, relative to
       that side's watch spot anchor (`StreamFraming.FromWorld`); logs
       `Stream camera framing saved Side= Offset= Pitch= Yaw= AngleRead=`.
       With no readable view angle the previous framing's angle is kept.
     - `Park` (still, not placed, or the side changed): parks at
       `StreamFramingStore.For(side)` (`Reason=framing`).
     - `Stay`: nothing.

## Invariants

- Never touches players, only the admin's own camera.
- No camera call while the admin moves the camera in fly cam.
- One framing definition for both sides: saved relative to the side's
  anchor, so it lands mirrored on the other side until that side gets its
  own.
- Logs camera moves in `lobby-*.log` as `Stream camera Reason=` (`keep`,
  `killer`, `any`, `retry`, `park`, `repark`, `framing`), plus framing
  saved and follow did not take.

## Dangerous Deadworks constraints

- The client starts in the directed view; a park only moves the camera in
  fly cam (C). The server cannot switch it: the client refuses
  `spec_mode` / `spec_player` from the server, so no client commands are
  sent.
- Whether a server follow pulls the client out of fly cam is not known yet
  (`follow did not take ... FlyCam=True` answers it).
- Whether the observer's `v_angle` tracks the fly cam view is not known
  yet: check `ViewAngle=` in `/spec_status` while turning, and
  `AngleRead=` on the framing saved line.
- Moving the camera while the admin also moved it crashed the client; so
  did following a hero spawned under a second earlier.
