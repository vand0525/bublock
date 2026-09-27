# SpectateRule

Pure decisions for an automatic spectator camera. No game calls, so it is
unit tested (`Tests/Modules.Tests/SpectateRuleTests`).

## Types

- `SpectateReason`: `Keep`, `Killer`, `Any`, `Park`.
- `SpectateChoice(Reason, Target)`: the reason and the Steam ID to follow
  (null for `Park`).

## Operations

| Operation | Result |
|---|---|
| `Choose(currentId, killerId, candidates)` | `Keep` the current target if it is a candidate; else `Killer` if the killer is a candidate; else `Any` (the first candidate); else `Park`. |
| `LookDown(yaw)` | The straight-down view angle: pitch `StraightDownPitch` (89), the given yaw, roll 0. |

## Invariants

- `candidates` are the players that may be watched right now (alive, in
  play). The caller shuffles them, so `Any` is random; the rule itself is
  deterministic.
- A dead victim is never chosen because the caller leaves dead players out
  of `candidates`, even when the victim is also the killer (suicide).
- Pitch is 89, not 90: Source caps view pitch at 89.
