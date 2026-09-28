# TeamsTests

Unit tests for `Modules/Teams` (`DeadlockTeams`, `ChoiceGuard`).

- `SmallerTeam` fills the team with fewer players and ignores spectators
  and other numbers; a tie gives Amber or Sapphire (both seen over 50
  seeds).
- `Name`: Amber, Sapphire, Spectator, `Team N`; `Other`; `IsPlayable`.
- `ChoiceGuard.Blocks`: `selecthero` only when the hero is locked (trimmed,
  any case), `changeteam` / `jointeam` only when the team is locked, other
  commands never.
