# AutoStartRule

Pure decision for match auto-start (Stage 13d). Unit tested.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Decide(enabled, running, humans, minPlayers = 2, leaving = false)` | Disabled → `None`. Not running, `humans >= minPlayers` and not `leaving` → `Start`. Running and `humans < minPlayers` → `End`. Otherwise `None` | `AutoStartAction` |

`AutoStartAction` is `None`, `Start`, or `End`. `DefaultMinPlayers` is 2.

## Invariants

- No side effects; `AutoStartService` counts players and acts on the result.
- A disconnect (`leaving`) never starts a match, only ends one: starting
  while the leaving controller is being removed crashed the server
  (2026-09-27 playtest). The next join or reload check starts it.
