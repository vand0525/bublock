# SoulRule

Pure rule for which soul (gold) gains are blocked. Souls earned mid-round
made one team snowball: kill bounties and passive income grow with the game
clock, which keeps running for hours on this server (2026-09-27 2v2
playtest). While a match runs, a player's power comes only from the round's
build.

## Operations

| Op | Returns |
|---|---|
| `ShouldBlock(type, source, amount, matchRunning)` | true when a match is running, `type` is `ECurrencyType.EGold`, `amount > 0`, and `source` is not `ECheats`, `EStartingAmount`, or `EItemSale` |

## Invariants

- Only gains are blocked; spending and losses (`amount <= 0`) always pass.
- `ECheats` stays allowed: loadouts run `ModifyCurrency(EGold, 0, ECheats)`
  to recalculate stats. `EStartingAmount` is the grant `ResetHero` triggers
  (the loadout then sets gold itself). `EItemSale` keeps selling working.
- Ability points and other currency types are never blocked.
- With no match running (lobby, 1v1 setup) nothing is blocked, so the 1v1
  setup souls and buying still work.
- No game calls; linked into `RiftRoulette.Tests` (`SoulRuleTests`).
  `GameLoopPlugin.OnModifyCurrency` applies it.
