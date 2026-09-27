# LoadoutSnapshot.cs

Plain records describing one player's exact hero state, used to copy a hero
from one player onto another (`LoadoutService.Capture` / `ApplySnapshot`).

## Types

- `SnapshotAbility(Name, Slot, UpgradeBits)` — one signature ability (slots
  `Signature1`–`Signature4`) with its exact upgrade bit mask.
- `SnapshotItem(Name, ImbuedAbilities)` — one owned item (`upgrade_*`) in the
  pawn's ability order, plus the names of the abilities it is imbued onto.
- `LoadoutSnapshot(Hero, Level, AbilityPoints, AbilityUnlocks, Abilities,
  Items, Source)` — the whole copy. `Source` is the source player's name, for
  logs only. `Describe()` gives a one-line summary (upgrade bits in binary).
- `SnapshotResult(ItemsAdded, ItemsFailed, Imbued, AbilitiesSet,
  AbilitiesMissing)` — what `ApplySnapshot` managed to set.

## Invariants

- No game calls; records are immutable once captured.
- Snapshots are not filtered: banned items and the soul cap do not apply
  (a copy is exact).
