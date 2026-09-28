# __NAME__Service

__TITLE__ as a composition of engine modules; the plugin classes only
forward hooks here. Static, one per DLL load.

| Op | Behavior |
|---|---|
| `ApplyServerConvars` | Team size 6, duplicate heroes, no rift (`citadel_koth_enabled 0`), no shop, 1 s respawn, the arena's active lane |
| `Admit` / `Remove` | Smaller team on join; session check 2 s later / on leave |
| `BlocksCommand` | Team changes refused (`ChoiceGuard`); heroes are the players' choice |
| `BlocksCurrency` | Nothing blocked yet (`SoulRule` with `active: false`) |
| `OnSpawn`, `ContainPlayers` | Arena spot on spawn; strays sent back every second |
| `OnDeath` | `__NAME__Rules.Credits` → `Session.Scores.Add` |
| `DescribeFor`, `Describe` | `/points` and status lines |
