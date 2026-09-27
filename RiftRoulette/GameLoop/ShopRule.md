# ShopRule

Pure rule for when players may buy items.

## Operations

| Op | Returns |
|---|---|
| `BuyAnywhere(isDuel, matchRunning)` | true only in 1v1 mode with no match running (the 1v1 setup window, when `DuelService.EnterSetup` hands out 100k souls) |

## Invariants

- CleanSlate disables every shop buy zone, so outside this window nobody can
  buy at all (Random and Draft included). Random builds and the 1v1 copy use
  `pawn.AddItem`, which does not need buying.
- No Deadworks calls; linked into `RiftRoulette.Tests` (`ShopRuleTests`).
