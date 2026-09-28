# Economy module

## Purpose

Currency rules a game type applies from its `OnModifyCurrency` hook. Today
one rule: block earned souls so power comes only from given builds. Added
in the redock fork with Gun Game.

## Files

| File | Role |
|---|---|
| `SoulRule.cs` | Which gold / ability-point gains to block (pure, tested) |
| `Economy.projitems` | Compiles it into a consumer |

## Public operations

See `SoulRule.md`.

## State

None.

## Commands and lifecycle

No plugin class. The game type calls `SoulRule.ShouldBlock` in
`OnModifyCurrency` and returns `HookResult.Stop` when it says so.

## Dependencies

`DeadworksManaged.Api` enums only.

## Later

Bounties, per-kill soul rewards, or starting gold per game type belong
here as more rules.
