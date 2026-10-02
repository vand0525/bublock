# StreamCam

Automatic stream camera for a seated admin (observer pawn). Runs by itself
as soon as the admin is seated: no command needed. Camera calls go through
`Modules/Spectate`; this file decides what to show.

- Players fighting: follow one (in-eye), cut to the killer when they die.
- A player just turned to stone (`ShowStatue`, from
  `BanStatueService.Petrify`): for `SpotlightHold` (10 s) the statue is the
  only one followed, even between rounds.
- Nobody fighting (between rounds, the wait for the rift to spawn, the
  lobby: everyone up top is restrained): park at the fixed spot for the
  current watch spot side (`StreamFraming.Spot(WatchSpot.Side)`: the next
  rift between rounds, the rift being fought during a round).
- Never moves the camera while the admin is moving it.

## State (per admin Steam ID, static)

- `Auto` (default on), `SeatedAt` (set by `Seated`).
- Follow: `LastFollowed`, `PendingKiller`, `FollowSentAt`,
  `FollowFailLogged`.
- Park: `ParkedSide` (cleared by a follow), `LastParkAt`, `ParkTarget`
  (world position sent), `ParkPending` (a fly cam park not yet checked),
  `Placed` (the fly cam park landed and has not been moved since),
  `Handled` (the admin moved the camera after it landed).
- `LastPosition` / `LastAngles`: the observer's position and view angle at
  the last update, to see the admin moving it.
- `SpawnedAt` (per player Steam ID): last `player_spawn`, from `NoteSpawn`.
- `_spotlight` (one, shared): the statue's Steam ID and until when; a
  newer ban replaces it, and it ends early once the player is no longer a
  statue (kicked, left).
- `Forget(steamId)` resets everything but `Auto` and drops the spawn time
  (stand up, roam, disconnect). A hot reload clears it (static);
  `AdminSeat.Restore` keeps a spectating admin seated.

## Operations

| Operation | Effect |
|---|---|
| `Tick(timer, mode)` | Every `TickSeconds` (2 s) from `LobbyPlugin.OnLoad`: for every seated admin who is observing with `Auto` on and past `SeatGrace` (3 s after `Seated`), runs the update below. |
| `Seated(steamId)` | Called by `AdminSeat.BecomeObserver` right after `MakeObserver`; starts the seat grace. |
| `NoteSpawn(steamId)` | From the `player_spawn` handler: records the spawn time for the follow delay. |
| `OnDeath(victim, attacker, timer, mode)` | If an admin's camera follows the victim, stores the attacker as `PendingKiller` (none for a suicide or a non-player) and runs `Tick` on the next tick. |
| `ShowStatue(steamId)` | Sets the spotlight to that statue until `SpotlightHold` (10 s) from now. |
| `SetAuto(admin, on, mode)` | Sets `Auto` and resets the rest of that admin's state. |
| `Describe(admin)` | Three lines for `spec_status`: auto, seat, observer mode, fly cam, the view angle read (or `unreadable`); who is watched, parked side, placed, handled, watch side; the spot (position and angle) for the current watch side. |

## Update (one admin)

Fly cam is `SpectateService.IsFlyCam` (observer `Roaming`, no target; only
the admin pressing C puts the client there). "Moved" means, in fly cam,
more than 50 units or 3 degrees since the last update
(`SpectateRule.HandMoved`, on the observer's position and `v_angle`).

1. A fly cam park sent: wait `ParkSettle` (1.25 s) for its teleport and
   angles, then check it once: within `PlacedUnits` (100) of the target
   and still in fly cam means `Placed`. Nothing else that update.
2. Candidates: during a spotlight, only the statue once its hero pawn is
   alive and at least `FollowGrace` (5 s) old (a rejoining statue gets a
   new lobby pawn). Otherwise participants (`Participants.Humans()`) with
   a live hero pawn spawned at least `FollowGrace` ago and not restrained
   (fighting), shuffled.
3. Candidates: follow (clears `ParkedSide`, `Placed`, `Handled`).
   - Moved: do nothing this update.
   - A follow was sent but the camera is not on that player (still a
     candidate): logs Information `Stream camera follow did not take
     Target= Mode= FlyCam=` once, and sends it again after `FollowRetry`
     (10 s, logged at Debug as `Reason=retry`).
   - Else `SpectateRule.Choose(current, PendingKiller, candidates)`:
     `Keep` (a manual click to another live player is adopted, logged once
     `Reason=keep`), `Killer` / `Any` (`SpectateService.Follow`, logged
     `Reason=killer|any Target= Accepted= Mode=`).
4. No candidates: park. Outside fly cam `Placed` / `Handled` are cleared;
   in fly cam, moving a `Placed` camera sets `Handled`. Then
   `StreamCamRule.ParkStep(flyCam, ParkedSide == side, Placed, Handled,
   moved, since last park, ReparkEvery 6 s)`:
   - `Park` (new side, or after a follow): logged Information
     `Stream camera Reason=park Side= FlyCam=`.
   - `Repark` (outside fly cam every 6 s, ready for when the admin presses
     C; in fly cam when the park did not land): logged at Debug.
   - `Stay`: nothing. A camera the admin moved after it landed stays where
     they left it until the side changes or a round starts.
   - The park is `SpectateService.Park(admin, spot.Position, spot.Angle)`:
     the spot's angle is handed over unchanged (that call teleports with no
     angle, then sends the client camera angle at 0.5 s and 1.0 s).

## Invariants

- Never touches players, only the admin's own camera.
- No camera call while the admin moves the camera in fly cam.
- Nothing is saved: the spots are fixed in `StreamFraming`.
- Logs camera moves in `lobby-*.log` as `Stream camera Reason=` (`keep`,
  `killer`, `any`, `retry`, `park`, `repark`), plus follow did not take.

## Dangerous Deadworks constraints

- The client starts in the directed view; a park only moves the camera in
  fly cam (C). The server cannot switch it: the client refuses
  `spec_mode` / `spec_player` from the server, so no client commands are
  sent.
- Whether a server follow pulls the client out of fly cam is not known yet
  (`follow did not take ... FlyCam=True` answers it).
- Moving the camera while the admin also moved it crashed the client; so
  did following a hero spawned under a second earlier.
