# SpectateRule

Pure decisions for an automatic spectator camera. No game calls, so it is
unit tested (`Tests/Modules.Tests/SpectateRuleTests`).

## Types

- `SpectateReason`: `Keep`, `Killer`, `Any`, `Park`.
- `SpectateChoice(Reason, Target)`: the reason and the Steam ID to follow
  (null for `Park`).
- `FlyCamAction`: `Wait`, `Park`, `Stay`, `Manual` (what to do with a
  viewer in fly cam with no target).
- `FlyCamSettle`: 1.5 s, how long a fly cam must stay still before the
  camera parks it. Under the 2 s camera tick, so the tick after first
  sight parks.

## Operations

| Operation | Result |
|---|---|
| `Choose(currentId, killerId, candidates)` | `Keep` the current target if it is a candidate; else `Killer` if the killer is a candidate; else `Any` (the first candidate); else `Park`. |
| `LookDown(yaw)` | The straight-down view angle: pitch `StraightDownPitch` (89), the given yaw, roll 0. |
| `ParkCheck(roaming, hasTarget, distance, tolerance)` | True only when the observer is roaming, has no observer target, and is within `tolerance` of the park spot (inclusive). |
| `IsManualMove(roaming, hasTarget, distance, tolerance)` | True when the observer is roaming with no target but farther than `tolerance`: the viewer flew away, the park did not fail. |
| `ManualActive(until, now)` | True while `now` is before the manual hold's end; false with no hold. |
| `FollowReady(spawnedAt, now, grace)` | True when no spawn was seen or at least `grace` has passed since it. |
| `FlyCamStep(confirmed, parkSent, atSpot, moved, firstSeen, now, settle)` | For a viewer in fly cam with no target. `confirmed` (a park was seen holding): `Stay` at the spot and still, else `Manual` (the viewer is moving or flew away). `parkSent` (park sent, not yet seen holding): `Stay` at the spot (the caller confirms it), else `Wait` (the caller restarts the settle). Otherwise `moved` since the last check: `Manual`; no `firstSeen`: `Wait`; still for at least `settle`: `Park`; else `Wait`. |

## Invariants

- `candidates` are the players that may be watched right now (alive, in
  play). The caller shuffles them, so `Any` is random; the rule itself is
  deterministic.
- A dead victim is never chosen because the caller leaves dead players out
  of `candidates`, even when the victim is also the killer (suicide).
- Pitch is 89, not 90: Source caps view pitch at 89.
