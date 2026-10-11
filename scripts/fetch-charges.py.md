# fetch-charges.py

Extracts ability charge data from the game's `scripts/abilities.vdata`
(GameTracking-Deadlock) into `RiftRoulette/Round/Data/ability-charges.json`,
which is embedded in `RiftRoulette.dll` and read by `Round/ChargeTable`.
Offline data step: the plugin never downloads anything.

## Usage

```bash
python3 Bublock/scripts/fetch-charges.py                          # download the latest abilities.vdata
python3 Bublock/scripts/fetch-charges.py --vdata /tmp/abilities.vdata  # parse a local copy
```

Then run the tests (`test.sh`), rebuild (`update.sh`) and deploy
(`deploy.sh --confirm`, with approval). Rerun after every game patch
(`reference/patch-day.md` step 7).

## Behavior

1. Downloads `abilities.vdata` from the GameTracking-Deadlock `master`
   branch (about 8 MB) and reads the commit that last changed it (GitHub API;
   a failure only leaves `commit` null).
2. Splits the file into top-level blocks (`\t<name> = `).
3. Abilities (names not starting with `upgrade_`): base `AbilityCharges`
   (`m_strValue`), the `AbilityCharges` bonus of each of the 3
   `m_vecAbilityUpgrades` tiers (`m_vecPropertyUpgrades`), and `conditional`
   (`AbilityChargesConditionally` = 1: charges come only from a resource
   pool filled by damage, like Wraith's cards and Graves' skulls). Kept only
   when the base or a tier is above 0, so abilities that never hold charges
   (Werewolf's `ability_werewolf_transformation`, base 0) are not listed.
4. Items (`upgrade_*`): `BonusAbilityCharges` above 0, skipping imbue-only
   items (`IMBUE` in the block, e.g. Omnicharge Pendant), disabled items
   (`m_bDisabled = true`) and `IGNORED_ITEMS` (Cultist Sacrifice, banned
   from every build). Today: Extra Charge +1, Rapid Recharge +2.
5. Stops with an error when no abilities or no items were found (the file
   layout changed).

## Output

```json
{
  "fetchedAt": "2026-10-11T19:20:00Z",
  "source": "https://github.com/SteamTracking/GameTracking-Deadlock/blob/master/game/citadel/pak01_dir/scripts/abilities.vdata",
  "commit": "2d87734ca566",
  "abilities": { "ability_baba_bubbling_brew": { "base": 1, "tiers": [0, 0, 1], "conditional": false } },
  "items": { "upgrade_extra_charge": 1, "upgrade_rapid_recharge": 2 }
}
```

## Side effects

- Overwrites `RiftRoulette/Round/Data/ability-charges.json`. No server access.
