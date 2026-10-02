# MatchConfig

Match configuration set by the server. Static, in memory: it
resets to the defaults on every DLL load (deploy or hot reload).

## Settings

| Setting | Values | Default | Effect |
|---|---|---|---|
| `HeroMode` | `Random`, `Draft`, `Duel`, `Mirror` | `Random` | Random: each intermission, every player gets a new random hero and build (`Random/RandomModeService`); pool boards and pick commands are off. Draft: the hero draft (`Draft/`, pool boards and `/pick`). Duel (alias `1v1`): one copied build on both players (`Duel/DuelService`); pool boards and pick commands are off. Mirror: each intermission one hero and one build, picked once (or pinned with `/mirror_hero` / `/mirror_build`), go to every fighter (`Mirror/MirrorModeService`); pool boards and pick commands are off. |
| `Format` | `Continuous` | `Continuous` | Continuous: rounds loop until `/match_end`. Other formats (best of N) come later. |

## Operations

| Op | Behavior |
|---|---|
| `SetHeroMode(mode)` / `SetFormat(format)` | Plain setters. Use `MatchService.SetHeroMode` / `SetFormat`, which refuse during a match and log. |
| `IsRandom` / `IsDuel` / `IsMirror` | `HeroMode == Random` / `Duel` / `Mirror` |
| `UsesDraft` | `HeroMode == Draft`; where "draft is off" matters (pick refusals, boards, team keeping on reset, stats boards) code checks this rather than `IsRandom` |
| `TryParseHeroMode(text, out mode)` / `TryParseFormat(text, out format)` | Case-insensitive names only; numbers and unknown names are rejected (pure). `TryParseHeroMode` also accepts `DuelAlias` (`1v1`, any case) as `Duel` |
| `Names<T>()` | Lowercase names joined by `\|` (for usage text) |
| `Describe()` / `Describe(mode, format)` | `Mode=random \| Format=continuous` |

## Consumers

`GameLoop/MatchService` (loop and commands), `GameLoop/AutoStartService`,
`Draft/DraftService` (boards and pick gating), `Stats/StatsService`,
`Random/RandomModeService`, `Duel/DuelService`, `Mirror/MirrorModeService`,
`Lobby/LobbyService`.
