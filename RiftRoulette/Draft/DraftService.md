# DraftService

Core draft operations: pick, unpick, reset, hero enforcement, starting
progression, board redraw, and draft listings. Static; no plugin-class
dependency. Ops that act on the next tick take an `ITimer` from the calling
plugin class. Extracted from Legacy in Stage 9; each op keeps the archive
order of effects.

## Logs

`Draft` feature log (`draft-YYYYMMDD.log`). Ops take
`ExecutionMode mode = Clean`; player lines carry a `PlayerRef`.

## Operations

| Op | Behavior | Returns |
|---|---|---|
| `CanChangeHero(player)` | Hero pawn exists and `IsAlive` | `bool` |
| `Pick(player, heroName, timer, mode)` | Archive `/select`. Refusals in order: draft off (`!MatchConfig.UsesDraft`: `NoDraftReply`, which is `RandomModeReply` or, in 1v1 mode, `DuelModeReply`), dead, unknown hero (`Enum.TryParse`, ignore case), caller already picked, hero already drafted, hero in neither pool. Success: `DraftState.Add`, `ChangeTeam(team)`, `SelectHero(hero)`, next tick `GiveStartingProgression`, `RedrawBoards` | archive chat line for the outcome |
| `GiveStartingProgression(player, mode)` | If the pick is still held and a pawn exists: gold set to 25,000. Nothing else (the archive log also said "24 AP" but granted none; the new log line states the gold only) | — |
| `Unpick(player, timer, mode)` | Archive `/unselect`. Draft off: refuse (`NoDraftReply`). No pick: refuse. Dead: refuse and log. Otherwise zero gold / AP / level, release the pick, `ChangeTeam(2, true)`, `SelectHero(Skyrunner)`, next tick `WatchSpot.SendUp` (restrain + watch spot), `RedrawBoards` | archive chat line |
| `Reset(timer, mode)` | Archive `ResetDraft`. Clears all picks; for each participant (`Participants.Humans()`: no bots, no seated admin) with a pawn: dead → Debug log and skip; alive → zero gold / AP / level, `ChangeTeam(2, true)` (Draft mode only; Random and 1v1 mode keep each player's team so teams stay even between matches), `SelectHero(Skyrunner)`, next tick `WatchSpot.SendUp` (restrain + watch spot). Then `RedrawBoards`; logs `Draft reset` | players reset |
| `EnforceHero(player, pawn, timer)` | Archive `player_hero_changed`. Non-participants (seated admin): nothing. Then `DuelService.GuardHero` (1v1 mode: always handled, so setup allows free hero switching; during a 1v1 match a swap kills and restores, see `Duel/DuelService.md`), then `RandomModeService.GuardHero` (Random mode players with an assignment: kill and rebuild on respawn, see `RandomModeService.md`); if it handled the player, stop. Otherwise expected hero = pick or Skyrunner. Wrong hero and dead: Debug log, no `SelectHero`. Wrong hero and alive: `SelectHero(expected)`. Right hero and no pick: zero gold / AP / level | — |
| `RedrawBoards(mode)` | `WorldTextService.ClearAll()`, then `draft.welcome` ("RIFT ROULETTE"), `draft.note` under it (`BoardLayout.Note(WelcomeNote)`, skipped when `WelcomeNote` is empty; currently the apology to Bomer and TimeBucket, the two players on the server when the admin seat crashed it on 2026-09-27) and the side boards `draft.sapphire` / `draft.amber` (text from `DraftBoardText.TeamBoard`); positions (anchored at the current watch spot), angles, colors and scales come from `BoardLayout`. `Round/WatchSpot.RefreshBoards` calls this when the watch spot changes side. When the draft is off (`!MatchConfig.UsesDraft`: Random and 1v1 mode) the pool boards are replaced by the stats boards (`StatsService.RefreshBoards`) in the same spots, plus the betting board (`BettingService.RefreshBoard`, Random mode only) | — |
| `DescribeHeroes()` | Player `/heroes`: `[NoDraftReply]` when the draft is off, else `DescribePools()` | lines |
| `DescribePicks()` | `Picks: Shiv (Theo, slot 3), Fencer (offline)` in drafted order, or `Picks: none` | line |
| `DescribePools()` | `DraftBoardText.PoolLine` for Sapphire and Amber, taken heroes marked | 2 lines |
| `DescribeDraft()` | Pool lines, pick count, then `Hero \| Name \| slot N \| SteamID=` per pick (`(offline)` if the picker is not connected) | lines |

`RandomModeReply` (public const): "Heroes are random this match - you get
a new hero and build every round." Random mode (Stage 13b) also writes its
assignments into `DraftState`; since Stage 13c its hero guard decides
enforcement for assigned players.

## Dangerous constraints

- Never call `SelectHero` while the player is dead (`CanChangeHero`,
  `EnforceHero` dead check).
- `RedrawBoards` clears **every** `point_worldtext` on the map (archive
  behavior, kept until parity sign-off).
- Timer callbacks capture the controller; `TeleportTo` skips players
  without a pawn.
- Game-thread only.
