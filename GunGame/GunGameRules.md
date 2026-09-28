# GunGameRules

Gun Game's own rules and player-facing text. Pure; unit tested in
`Tests/GunGame.Tests`. Everything else Gun Game does comes from engine
modules (`GunGameService.md`).

## Settings

| Name | Value |
|---|---|
| `Title` | `Gun Game` |
| `Session` | `SessionOptions(MatchSeconds: 120, BreakSeconds: 5, MinPlayers: 2, WarningSeconds: 10)` |
| `ArenaResource` | `GunGame.arena.json` (the embedded `Data/arena.json`) |

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `Credits(attacker, victim, attackerTeam, victimTeam)` | A point when the attacker is a real player (Steam ID not 0), not the victim, on a playing team, and not on the victim's team | bool |
| `StartBanner(seconds)` | `Gun Game` / `2:00 - most kills wins` | (title, description) |
| `WarningBanner(secondsLeft)` | `10 seconds left` / `Most kills wins` | (title, description) |
| `KillBanner(kills, hero, build, souls)` | `3 kills: Haze` / `<build> - 12,345 souls` | (title, description) |
| `HeroBanner(hero, build, souls)` | `Haze` / `<build> - 12,345 souls` (join, admin reroll) | (title, description) |
| `ResultBanner(result, nameOf, breakSeconds)` | `<name> wins` / `7 kills - next match in 5s`; ties `Tie: A, B` / `5 kills each - ...`; nobody scored `Match over` / `No kills - ...` | (title, description) |
| `WaitingLine(players, min)` | Chat line while waiting for players | string |
| `BuildDescription(build, souls)`, `Kills(n)` | `<build> - 12,345 souls` (invariant culture); `1 kill` / `N kills` | string |

## Invariants

- Only kills score. Bots may be victims; the scoring side is a Steam ID.
- Banners only for what players need: match start, 10 s left, their new
  hero after a kill, the result (`.rules` §0.2 banner rule, applied here).
