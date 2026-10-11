# ChargeFill

Sets a hero's charged signature abilities to their full charges. Static;
no state. Game-thread only.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `RemainingCharges` | `SchemaAccessor<int>` for `CCitadelBaseAbility.m_iRemainingCharges`; also listed in the self-test's schema check | accessor |
| `Fill(pawn)` | Held items = `AbilityName` of every valid `IsItem` ability on the pawn. For `Signature1`..`Signature4`: skips empty slots, abilities not in `ChargeTable`, and those `ChargeTable.ShouldFill` refuses (resource-pool ultimates); `max = ChargeTable.Max(entry, UpgradeBits, heldItems, items)`; when `RemainingCharges < max`, writes `max` through `RemainingCharges.Set` (which also notifies network state so the HUD updates) | `name before->after` per filled ability, joined with `; ` (empty when nothing changed) |

## Callers

- `RoundFlow.MoveTeamsToRift` (`ReadyToFight`), after the heal and
  `ResetAllAbilityCooldowns`, for every fighter released into the rift.

## Invariants

- Only raises charges, never lowers them: a table max that is too low costs
  nothing; an imbued Omnicharge Pendant's extra charges are kept.
- Werewolf's wolf form is never filled (not in the table, and
  `ShouldFill` refuses resource-pool ultimates).

## Dangerous constraints

- Deadworks' own refill (`ResetCooldown` / `ResetAllAbilityCooldowns`)
  adds no charges on the current game build (its `UsesCharges` check reads
  false for every ability), and `MaxCharges` is unreliable; that is why the
  max comes from `ChargeTable` and the field is written directly.
- The write is not capped by the game: a table max that is too high gives
  the fighter extra charges until they spend them. Rerun
  `fetch-charges.py` after a patch.
- `m_iRemainingCharges` is a schema field; the self-test checks it still
  resolves (`SelfTestService`, `patch-day.md` section 5).
