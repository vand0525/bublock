# AbilityCharges

Read-only description of a hero's four signature abilities, for the logs.
Static; no state. Changes nothing on the pawn.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Describe(pawn)` | For `Signature1`..`Signature4` (`AbilityComponent.GetAbilityBySlot`, empty or invalid slots skipped): `name charges=<RemainingCharges> max=<MaxCharges> cd=<seconds left>s`, joined with `; `. Cooldown left is `CooldownEnd - GlobalVars.CurTime`, never below 0 | text |

## Callers

- `RoundFlow.MoveTeamsToRift` (`ReadyToFight`): before and after the
  ability reset at release.
- `GameLoop/MatchProbe`: each player's probe line (`moved-in` is 1 s after
  release).

## Dangerous constraints

- `RemainingCharges` reads the schema field `m_iRemainingCharges`.
  `MaxCharges` comes from a Deadworks native that calls the game's
  `UsesCharges` / `GetMaxCharges` virtuals; on game builds where those
  indices are stale it reads 0 for every ability (seen 2026-10-11), so
  `max=0` alone does not mean the ability has no charges.
- Wraith's Card Trick, Graves' Haunting Skull and a few others get charges
  only from a resource pool filled by damage (`AbilityChargesConditionally`);
  no cooldown reset fills them.
- Game-thread only.
