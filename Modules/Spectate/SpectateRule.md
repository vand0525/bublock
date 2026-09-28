# SpectateRule

Pure decisions for an automatic spectator camera. No game calls, so it is
unit tested (`Tests/Modules.Tests/SpectateRuleTests`).

## Types

- `SpectateReason`: `Keep`, `Killer`, `Any`, `Park`.
- `SpectateChoice(Reason, Target)`: the reason and the Steam ID to follow
  (null for `Park`).
- `FramingAction`: `Wait`, `Adjust`, `Save`, `Park`, `Stay` (what to do
  with a parked fly cam, see `FramingStep`).

## Constants

- `StraightDownPitch` 89 (Source caps view pitch at 89).
- `MoveUnits` 50 and `TurnDegrees` 3: how far the view must move or turn
  between two checks to count as the viewer moving it.

## Operations

| Operation | Result |
|---|---|
| `Choose(currentId, killerId, candidates)` | `Keep` the current target if it is a candidate; else `Killer` if the killer is a candidate; else `Any` (the first candidate); else `Park`. |
| `LookDown(yaw)` | The straight-down view angle: pitch `StraightDownPitch` (89), the given yaw, roll 0. |
| `FollowReady(spawnedAt, now, grace)` | True when no spawn was seen or at least `grace` has passed since it. |
| `Turned(from, to)` | The larger of the pitch change and the yaw change (yaw wrapped, so 179 to -179 is 2 degrees). |
| `WrapDegrees(degrees)` | The angle in [-180, 180). |
| `HandMoved(distance, turned)` | True when the view moved more than `MoveUnits` or turned more than `TurnDegrees`. |
| `FramingStep(placed, adjusting, moved, spotChanged)` | `moved`: `Adjust` when the camera had been placed (or was already being adjusted), else `Wait` (the viewer is flying somewhere; never move the camera under them). Still and `adjusting`: `Save` (the viewer moved it from the placed spot and let go: that is the new framing). Still, not placed or the spot changed: `Park`. Otherwise `Stay`. |

## Invariants

- `candidates` are the players that may be watched right now (alive, in
  play). The caller shuffles them, so `Any` is random; the rule itself is
  deterministic.
- A dead victim is never chosen because the caller leaves dead players out
  of `candidates`, even when the victim is also the killer (suicide).
- `FramingStep` never returns `Park` or `Save` while the view is moving.
