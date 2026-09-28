# MatchConfig

Match configuration set by the server (Stage 13b). Static, in memory: it
resets to the defaults on every DLL load (deploy or hot reload).

## Settings

| Setting | Values | Default | Effect |
|---|---|---|---|
| `HeroMode` | `Random`, `Draft`, `Duel` | `Random` | Random: each intermission, every player gets a new random hero and build (`Random/RandomModeService`); pool boards and pick commands are off. Draft: the Stage 12 hero draft. Duel (Stage 13g, alias `1v1`): one copied build on both players (`Duel/DuelService`); pool boards and pick commands are off. |
| `Format` | `Continuous`, `GunGame` | `Continuous` | Continuous: rounds loop until `/match_end`. GunGame (Stage 13k, Random mode only): rounds loop as in Continuous, every credited kill gives the killer a new random hero and build and one step on the kill ladder, and the first to the target wins the match (`GunGame/GunGameService`). Other formats (best of N) come later. |

## Operations

| Op | Behavior |
|---|---|
| `SetHeroMode(mode)` / `SetFormat(format)` | Plain setters. Use `MatchService.SetHeroMode` / `SetFormat`, which refuse during a match and log. |
| `IsRandom` / `IsDuel` | `HeroMode == Random` / `Duel` |
| `IsGunGame` | `Format == GunGame` and `IsRandom`: the Gun Game format set in another hero mode does nothing |
| `UsesDraft` | `HeroMode == Draft`; where "draft is off" matters (pick refusals, boards, team keeping on reset, stats boards) code checks this rather than `IsRandom` |
| `TryParseHeroMode(text, out mode)` / `TryParseFormat(text, out format)` | Case-insensitive names only; numbers and unknown names are rejected (pure). `TryParseHeroMode` also accepts `DuelAlias` (`1v1`, any case) as `Duel` |
| `Names<T>()` | Lowercase names joined by `\|` (for usage text) |
| `Describe()` / `Describe(mode, format)` | `Mode=random \| Format=continuous` |

## Consumers

`GameLoop/MatchService` (loop and commands), `GameLoop/AutoStartService`,
`Draft/DraftService` (boards and pick gating), `Stats/StatsService`,
`Random/RandomModeService`, `Duel/DuelService`, `Lobby/LobbyService`,
`GunGame/GunGameService`.
