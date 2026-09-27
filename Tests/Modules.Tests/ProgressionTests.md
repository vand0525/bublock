# ProgressionTests

Unit tests for `Modules/Loadout/Progression` (pure).

- `ForSouls` returns the last row reached: below 600 souls the first row
  (level 1, 1 unlock); 800 is level 2 with the first point; 3,800 is the
  fourth unlock; 18,000 is level 24 with 20 points; 20,000 is level 25 with
  21 points; 49,200 and above is level 36 with 32 points. Boons are always
  `Level - 1`.
- `Max` / `MaxLevel`: level 36, 35 boons, 4 unlocks, 32 points.
- Unlocks arrive exactly at 600, 1,100, 2,000 and 3,800.
