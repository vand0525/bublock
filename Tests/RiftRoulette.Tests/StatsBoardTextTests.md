# StatsBoardTextTests

Unit tests for `Stats/StatsBoardText`.

- Full board text: header with rounds, team total, rows sorted by kills.
- `1 round` wording and `No players` for an empty team.
- Streak board: ranked by best (desc) then name, players with 0 left out,
  numbered from 1.
- Empty streak board (or only zeros): `No streaks yet`.
- `Trim` cuts names to 16 characters.
