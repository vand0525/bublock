# StatsLedgerTests

Unit tests for `Stats/StatsLedger`.

- An enemy kill gives the killer a kill, the victim a death, the assister an
  assist, and returns the killer's team with the credited assister.
- A death with no attacker counts only the death.
- Suicides and team kills give no kill.
- Assists are deduplicated and exclude the killer, the victim, and the other
  team; the returned assisters match the ones credited.
- `Score`, `Total`, `Line`, and `Reset`.
