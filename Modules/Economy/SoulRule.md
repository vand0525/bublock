# SoulRule

Which currency gains to block in a game type where power comes only from
the build a player is given. Pure; unit tested in `Tests/Modules.Tests`.
Adapted from Rift Roulette's `GameLoop/SoulRule` (same rule, generic
signature); Rift Roulette keeps its own copy.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `ShouldBlock(type, source, amount, active, ranksFromBuild = true)` | Nothing when not `active` or `amount <= 0`. Gold (`EGold`): blocked unless the source is `ECheats`, `EStartingAmount` or `EItemSale`. Ability points / unlocks: blocked unless `ECheats` when `ranksFromBuild` | bool |

## Use

`OnModifyCurrency` → `HookResult.Stop` when `ShouldBlock` is true.
`Modules/Loadout` sets gold and ranks through cheats, so builds still land.

## Invariants

- Spending (negative amounts) is never blocked.
