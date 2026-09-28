# EconomyTests

Unit tests for `Modules/Economy/SoulRule`.

- Gold from kills and level-ups is blocked; cheats (loadouts), the
  starting amount and item sales pass.
- Ability points pass only from cheats when ranks come from the build; with
  `ranksFromBuild: false` they pass.
- Nothing is blocked when inactive or when spending (negative amounts).
