# SoulRuleTests

Unit tests for `RiftRoulette/GameLoop/SoulRule`.

- During a match, gold from kills, assists, trooper orbs, team bonus and
  comeback bounties is blocked.
- `ECheats`, `EStartingAmount` and `EItemSale` gold passes.
- Without a match nothing is blocked.
- Spending and losses (0 or negative amounts) pass.
- Ability point and unlock gains from level-ups and the starting grant are
  blocked during a match (ranks come from the build).
- They pass without a match, from `ECheats`, and when spent.
- Other currencies (item enhancements) pass.
