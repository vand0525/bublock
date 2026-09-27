# SoulRuleTests

Unit tests for `RiftRoulette/GameLoop/SoulRule`.

- During a match, gold from kills, assists, trooper orbs, team bonus and
  comeback bounties is blocked.
- `ECheats`, `EStartingAmount` and `EItemSale` gold passes.
- Without a match nothing is blocked.
- Spending and losses (0 or negative amounts) pass.
- Other currencies (ability points) pass.
