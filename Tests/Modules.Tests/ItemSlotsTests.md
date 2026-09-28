# ItemSlotsTests

Unit tests for `Modules/Loadout/ItemSlots` (pure).

- `ForSouls`: 9 items below 16,000 (including 14,000), 10 from 16,000, 11
  from 22,000, 12 from 28,000 and above; each edge checked on both sides.
- `ExtraPasses`: false at 9 and 11 slots, true at 12.
- `Breakpoints` grow in souls and slots and end at `MaxSlots`.
- `Describe` lists every breakpoint.
