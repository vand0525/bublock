# MatchConfig

Match configuration set by the server. Static, in memory: it
resets to the defaults on every DLL load (deploy or hot reload).

## Settings

| Setting | Values | Default | Effect |
|---|---|---|---|
| `HeroMode` | `Random`, `Mirror` | `Random` | Random: each intermission, every player gets a new random hero and build (`RandomMode/RandomModeService`). Mirror: each intermission one hero and one build, picked once (or pinned with `/mirror_hero` / `/mirror_build`), go to every fighter (`Mirror/MirrorModeService`). |
| `Format` | `Continuous` | `Continuous` | Continuous: rounds loop until `/match_end`. Other formats (best of N) come later. |

## Operations

| Op | Behavior |
|---|---|
| `SetHeroMode(mode)` / `SetFormat(format)` | Plain setters. Use `MatchService.SetHeroMode` / `SetFormat`, which refuse during a match and log. |
| `IsRandom` / `IsMirror` | `HeroMode == Random` / `Mirror` |
| `TryParseHeroMode(text, out mode)` / `TryParseFormat(text, out format)` | Case-insensitive names only; numbers and unknown names are rejected (pure) |
| `Names<T>()` | Lowercase names joined by `\|` (for usage text) |
| `Describe()` / `Describe(mode, format)` | `Mode=random \| Format=continuous` |

## Consumers

`GameLoop/MatchService` (loop and commands), `Boards/BoardService`,
`RandomMode/RandomModeService`, `Mirror/MirrorModeService`,
`Lobby/LobbyService`, `Lobby/LobbyPlugin` (`/about`), `Betting/BettingService`.
