# SoulRule

Pure rule for which currency gains are blocked. Souls earned mid-round
made one team snowball: kill bounties and passive income grow with the game
clock, which keeps running for hours on this server (2026-09-27 2v2
playtest). While a match runs, a player's power comes only from the round's
build: its items, and the level and ability ranks that item value buys
(`Modules/Loadout/Progression`, `LoadoutPlanner.AbilityPrefix`).

## Operations

| Op | Returns |
|---|---|
| `ShouldBlock(type, source, amount, matchRunning, ranksFromBuild = false)` | false when no match is running or `amount <= 0`. Gold (`EGold`): true unless `source` is `ECheats`, `EStartingAmount`, or `EItemSale`. Ability points (`EAbilityPoints`) and unlocks (`EAbilityUnlocks`): true when `ranksFromBuild` and `source` is not `ECheats`. Every other currency: false. |

`GameLoopPlugin` passes `ranksFromBuild = !MatchConfig.UsesDraft`: Random
mode and 1v1 mode set ability ranks themselves (a budgeted loadout or a
copied snapshot) and set both wallets with `SetCurrency`.

## Invariants

- Only gains are blocked; spending and losses (`amount <= 0`) always pass.
- `ECheats` stays allowed: loadouts run `ModifyCurrency(EGold, 0, ECheats)`
  to recalculate stats. `EStartingAmount` is the grant `ResetHero` triggers:
  allowed for gold (the loadout then sets gold itself), blocked for ability
  points and unlocks so a late starting grant cannot refill the wallets
  the loadout zeroed. `EItemSale` keeps selling working.
- `ELevelUp` and `EAbilityPurchase` ability gains are blocked in Random and
  1v1 matches, so the ranks stay the build's prefix. Draft mode keeps them
  (a draft pick gets its starting unlock from the game).
- `SetCurrency` does not pass through this hook.
- With no match running (lobby, 1v1 setup) nothing is blocked, so the 1v1
  setup souls, buying and free ability upgrades still work.
- No game calls; linked into `RiftRoulette.Tests` (`SoulRuleTests`).
  `GameLoopPlugin.OnModifyCurrency` applies it.
