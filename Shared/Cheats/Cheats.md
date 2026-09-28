# Cheats

Runs an action with `sv_cheats` temporarily enabled.

## Behavior

- `Run(Action action)`: sets `sv_cheats` to `1`, runs `action`, then sets
  `sv_cheats` to `0` in a `finally` block (also on exceptions).
- If the `sv_cheats` convar is not found, `action` still runs.

## Side effects

- Writes the global `sv_cheats` convar. Always leaves it at `0`, even if it
  was `1` before the call.

## Invariants

- Synchronous only: work scheduled on a timer from inside `action` runs after
  cheats are already disabled.
