# ChargeTableTests

Unit tests for `RiftRoulette/Round/ChargeTable`, against the real embedded
`Round/Data/ability-charges.json` (rerunning `fetch-charges.py` after a
patch can move these numbers; update the expectations from the new data).

- The table loads; Extra Charge is +1 and Rapid Recharge +2.
- Baba's brew: fully upgraded with Rapid Recharge holds 4 (1 base, +1 at
  tier 3, +2); with one tier and no charge item, 1.
- Tier bonuses count only when unlocked: Mirage's beetles 2, or 3 with
  tier 2; Priest's bear trap and Hornet's snipe 2 when only unlocked.
- Wraith's cards (2, or 4 with tier 1) and Graves' skulls (4) are
  conditional and filled on a regular slot.
- Werewolf's `ability_werewolf_transformation` is not in the table (no max).
- `ShouldFill` refuses a conditional ability in `Signature4` only.
- Charge items do not stack: Extra Charge plus Rapid Recharge is +2.
- An unknown ability has no max; `Parse` refuses a table without items.
