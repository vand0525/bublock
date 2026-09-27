# StreamCam

Automatic stream camera for a seated admin (observer pawn). Runs by itself
as soon as the admin is seated: no command needed. Camera calls go through
`Modules/Spectate`; this file decides whom to watch and when.

## State (per admin Steam ID, static)

- `Auto` (default on), `LastFollowed` (Steam ID the camera was put on),
  `FallbackSent` (client command already sent for that target),
  `PendingKiller`, `Parked` / `ParkedSide`, `OverviewEnd`, `ReturnTo`
  (player to show after the top-down), `LastShownRound`.
- `Forget(steamId)` resets everything but `Auto` (stand up, disconnect).
- A hot reload clears it (static); `AdminSeat.Restore` re-seats the admin
  and the next tick starts over.

## Operations

| Operation | Effect |
|---|---|
| `Tick()` / `Tick(mode)` | Every `TickSeconds` (2 s) from `LobbyPlugin.OnLoad`: for every seated admin who is observing with `Auto` on, runs the update below. |
| `OnDeath(victim, attacker, timer, mode)` | If an admin's camera is on the victim (or the victim is the top-down return-to player), stores the attacker as `PendingKiller` (none for a suicide or a non-player) and runs `Tick` on the next tick. |
| `OnBigUlt(caster, ability, timer, mode)` | While a top-down view shows: the caster becomes `ReturnTo` (logged, not extended). Otherwise, if `OverviewRule.CanStart(RiftService.IsRunning, RiftService.RoundNumber, LastShownRound)`: records the round and starts the top-down view with the caster as `ReturnTo` (logged `Reason=ult Caster= Ability= Round=`). Else logs Information `Big ult skipped ...`. |
| `ShowOverview(admin, timer, mode)` | `spec_overview`: starts the top-down view now, returning to the current player; does not touch `LastShownRound`. Throws `CommandException` when not observing. |
| `SetAuto(admin, on, mode)` | Sets `Auto`, clears any top-down and the park flag. |
| `Describe(admin)` | Three status lines for `spec_status`. |

## Update (one admin)

1. Top-down showing: do nothing. Just ended: remember that it ended.
2. Candidates: participants (`Participants.Humans()`) with a live hero
   pawn, shuffled. If any of them are not restrained (fighting), only those.
3. Just ended and `ReturnTo` is a candidate: follow them (`Reason=caster`).
4. Otherwise `SpectateRule.Choose(current, PendingKiller, candidates)`,
   where current is the candidate the observer is on, else `LastFollowed`
   if still a candidate (the server target did not stick: the
   `spec_player` client command is sent once for it), and null right
   after a top-down.
   - `Keep`: stay; a manual click to another live player is adopted and
     logged once (`Reason=keep`).
   - `Killer` / `Any`: `SpectateService.Follow` (`Reason=killer|any`,
     `Accepted=`).
   - `Park`: top-down at `WatchSpot.Location(WatchSpot.Side)`, moved only
     when parking starts or the side changes (`Reason=park Side=`).

## Top-down view

- Position: the main watch spot anchor (`WatchSpot.Location(side)`, not a
  per-slot spot), above the rift being fought or the next one.
- Angle: `SpectateRule.LookDown(anchor yaw)`: pitch 89, the watch spot's
  yaw.
- Length: `OverviewRule.Duration` (10 s); a `timer.Once` runs `Tick` when
  it ends, so the cut back is on time.

## Invariants

- Never touches players, only the admin's own camera.
- At most one ult-triggered top-down per live round.
- Logs every camera move in `lobby-*.log` as `Stream camera Reason=`
  (`keep`, `killer`, `any`, `ult`, `caster`, `park`, `overview`).

## Dangerous Deadworks constraints

- Whether teleporting the roaming observer pawn moves the camera, and
  whether the server fires `player_used_ability`, are to confirm in game.
