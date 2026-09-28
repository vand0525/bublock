# __NAME__Rules

__TITLE__'s own rules and player-facing text. Pure; unit tested in
`Tests/__NAME__.Tests`.

| Op | Behavior |
|---|---|
| `Session` | `SessionOptions(120 s, 5 s break, 2 players, 10 s warning)` |
| `Credits(attacker, victim, attackerTeam, victimTeam)` | A point for an enemy kill by a player on a team |
| `StartBanner`, `WarningBanner`, `ResultBanner`, `WaitingLine` | The text players see |
