# ChargeTable

How many charges an ability should hold, from the game data embedded in
the DLL (`Round/Data/ability-charges.json`, written offline by
`scripts/fetch-charges.py` from the game's `abilities.vdata`). Pure: no
game calls, unit-tested by `Tests/RiftRoulette.Tests/ChargeTableTests`.
State: the parsed table, loaded once (lazy) from the embedded resource
`RiftRoulette.Round.ability-charges.json`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Data` | The embedded table (`abilities`, `items`) | `ChargeData` |
| `Find(ability)` | Table entry by ability class name | `ChargeEntry` or null |
| `Max(ability, upgradeBits, heldItems)` | `Find`, then the `Max` below with the embedded items | charges, or null when the ability is not in the table |
| `Max(entry, upgradeBits, heldItems, items)` | `Base` + the `Tiers` bonuses whose tier is unlocked (tier i when bit i + 1 of `UpgradeBits` is set; `0b1` = unlocked only, `0b11111` = all three tiers, as `LoadoutPlanner.BitsFor` writes them) + the **largest** `BonusAbilityCharges` among held items (Extra Charge +1, Rapid Recharge +2; they are one upgrade line and never stack) | charges |
| `ShouldFill(entry, slot)` | False for a `Conditional` (resource-pool) ability in `Signature4`; true otherwise | bool |
| `Parse(json)` | Deserializes the table; throws when `abilities` or `items` is missing | `ChargeData` |

## Invariants

- Abilities that never hold charges are not in the table (Werewolf's
  `ability_werewolf_transformation` has base 0), so `Max` is null for them.
- `ShouldFill` keeps resource-pool ultimates out even if a patch gives one
  base charges: they must be earned in the fight (Werewolf would start in
  wolf form).
- Conditional abilities on slots 1 to 3 are filled: Wraith's
  `citadel_ability_card_toss` (2, +2 at tier 1) and Graves'
  `ability_necro_hauntingskull` (4).
- Imbue-only charge items (Omnicharge Pendant) are not in the table; an
  ability it imbues may hold more than `Max`, and `ChargeFill` never
  lowers charges.

## Dangerous constraints

- The table goes stale with game patches: rerun `fetch-charges.py`
  (`reference/patch-day.md` step 7). A renamed ability simply gets no fill.
