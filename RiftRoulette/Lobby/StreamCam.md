# StreamCam

Automatic stream camera for a seated admin (observer pawn). Runs by itself
as soon as the admin is seated: no command needed. Camera calls go through
`Modules/Spectate`; this file decides what to show.

- Players fighting: follow one (in-eye), cut to the killer when they die.
- A player just turned to stone (`ShowStatue`, from
  `BanStatueService.Petrify`): for `SpotlightHold` (10 s) the statue is the
  only one followed, even between rounds.
- Nobody fighting (between rounds, the wait for the rift to spawn, the
  lobby: everyone up top is restrained): no camera call. The camera stays
  where it is (where the client put it on joining, or on the last player
  followed) until someone fights. Automatic parking at the fixed spot was
  removed on 2026-10-11: every client crash of a seated admin since
  2026-09-28 came right after a park (see `resources.md`).
- Never moves the camera while the admin is moving it.

## State (per admin Steam ID, static)

- `Auto` (default on), `SeatedAt` (set by `Seated`).
- Follow: `LastFollowed`, `PendingKiller`, `FollowSentAt`,
  `FollowFailLogged`.
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
| `Describe(admin)` | Three lines for `spec_status`: auto, seat, observer mode, fly cam, the view angle read (or `unreadable`); who is watched, watch side; the fixed spot (position and angle) for the current watch side (not parked at; kept for reference). |

## Update (one admin)

Fly cam is `SpectateService.IsFlyCam` (observer `Roaming`, no target; only
the admin pressing C puts the client there). "Moved" means, in fly cam,
more than 50 units or 3 degrees since the last update
(`SpectateRule.HandMoved`, on the observer's position and `v_angle`).

1. Candidates: during a spotlight, only the statue once its hero pawn is
   alive and at least `FollowGrace` (5 s) old (a rejoining statue gets a
   new lobby pawn). Otherwise participants (`Participants.Humans()`) with
   a live hero pawn spawned at least `FollowGrace` ago and not restrained
   (fighting), shuffled.
2. Candidates: follow.
   - Moved: do nothing this update.
   - A follow was sent but the camera is not on that player (still a
     candidate): logs Information `Stream camera follow did not take
     Target= Mode= FlyCam=` once, and sends it again after `FollowRetry`
     (10 s, logged at Debug as `Reason=retry`).
   - Else `SpectateRule.Choose(current, PendingKiller, candidates)`:
     `Keep` (a manual click to another live player is adopted, logged once
     `Reason=keep`), `Killer` / `Any` (`SpectateService.Follow`, logged
     `Reason=killer|any Target= Accepted= Mode=`).
3. No candidates: clears `LastFollowed` and `PendingKiller`; no camera
   call.

## Invariants

- Never touches players, only the admin's own camera.
- No camera call while the admin moves the camera in fly cam.
- No camera call while nobody is fighting (never parks).
- Logs camera moves in `lobby-*.log` as `Stream camera Reason=` (`keep`,
  `killer`, `any`, `retry`), plus follow did not take.

## Dangerous Deadworks constraints

- A park (`SpectateService.Park`: observer mode change, teleport, camera
  angle message) preceded every seated admin client crash from 2026-09-28
  to 2026-10-11 (12 of about 4,300 parks; 0 in about 1,100 follows), most
  often right after a state change: round end, the followed player
  leaving, or 3 s after going spectating. Which part crashes is not
  known; do not call it from here until that is worked out.
- The server cannot switch the client into fly cam: the client refuses
  `spec_mode` / `spec_player` from the server, so no client commands are
  sent.
- Whether a server follow pulls the client out of fly cam is not known yet
  (`follow did not take ... FlyCam=True` answers it).
- Moving the camera while the admin also moved it crashed the client; so
  did following a hero spawned under a second earlier.
