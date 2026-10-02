# Draft — Feature

## Purpose

The Rift Roulette hero draft: two hero pools, one pick per player, team
assignment from the pick, starting gold, hero enforcement, the draft boards,
and the pick commands.

The draft is active only when `GameLoop/MatchConfig.HeroMode`
is `draft`. In `random` (the default) the pool boards are hidden, pick
commands reply that heroes are random, and `RandomMode/RandomModeService`
writes its per-round assignments into `DraftState`. The draft code is kept
for later use.

In Random mode the stats boards (`Stats/`) take the pool boards'
spots (`BoardLayout`), `EnforceHero` defers to the Random mode hero guard,
and `Reset` keeps each player's team.

Hint and note boards: right under the welcome board `RedrawBoards` always
draws the fixed hint `Type /about to learn how to play and bet`
(`draft.hint`, `DraftService.AboutHint`). Under the hint it draws the
`WelcomeNoteStore` note (`draft.note`) whenever it is not empty. Both move
with the welcome board (watch spot) and are redrawn with it. `/draft_note` sets it; the text
is saved in `bublock/welcomenote.txt` on the server, so it survives
uploads and restarts.

## Files

| File | Role |
|---|---|
| `DraftState.cs` | Picks (Steam ID to hero) and the drafted-hero set (static, pure) |
| `DraftPools.cs` | Sapphire / Amber hero pools, `TeamOf` (pure) |
| `DraftBoardText.cs` | Board and pool-line text (pure) |
| `BoardLayout.cs` | Welcome, note (under the welcome) and side-board positions, angles, colors (pure) |
| `DraftService.cs` | Ops: `Pick`, `Unpick`, `Reset`, `EnforceHero`, `GiveStartingProgression`, `RedrawBoards`, `CanChangeHero`, `DescribePicks` / `DescribePools` / `DescribeDraft` |
| `WelcomeNoteStore.cs` | The note under the welcome board, saved in `bublock/welcomenote.txt` |
| `DraftPlugin.cs` | Hooks and commands (thin wrappers) |

## Public operations

See `DraftService.md`. Commands:

- Player: `/pick`, `/unpick`, `/picks`, `/heroes`.
- Admin: `/draft_status`, `/draft_assign`, `/draft_release`, `/draft_reset`,
  `/draft_boards`, `/draft_note`.

Catalogued in `reference/user-commands.md` and `reference/admin-commands.md`.

## State

`DraftState` (one set of picks per DLL load) and the `WelcomeNoteStore`
note (loaded from its file).

## Dependencies

- `Modules/WorldText` (boards), `Round/WatchSpot` (send players up, board
  anchor; never the fixed `draft` spot).
- `Lobby/RiftRouletteTeams` (team numbers).
- `Duel/DuelService.GuardHero`, `RandomMode/RandomModeService.GuardHero`
  and `Mirror/MirrorModeService.GuardHero` (hero locks), `Stats/StatsService.RefreshBoards` (when the draft is off).
- In 1v1 mode the draft is off like Random mode (`UsesDraft`
  checks) and `EnforceHero` never forces `LobbyHero`, so players can pick
  heroes from the menu while a build is prepared.
- `Shared` (`AdminCommand`, `PlayerChat`, logging).

## Consumers

- `Lobby/LobbyService`: releases a pick on disconnect / kick and calls
  `DraftService.RedrawBoards`; shows picks in `/status` and refuses
  `/player_team` for players with a pick.
- `Round/RoundFlow`: moves players who hold a `DraftState` pick to their
  team's rift start (grouped by `TeamNum`).
- `RandomMode/RandomModeService`: writes Random mode assignments into
  `DraftState`.
- `GameLoop/MatchService`: `Reset` on `/match_end` and on mode changes.
- `Stats/StatsService`: draws the Random mode stats boards with `BoardLayout.Side`.

## Logs

`draft-YYYYMMDD.log`: picks, refusals, unpicks, resets, starting gold,
enforcement skips while dead (Debug), board redraws (Debug), admin command
gate.

## Lifecycle vs commands

- Hooks (startup boards, hero enforcement) run in Clean mode.
- Player commands run the ops in Clean mode.
- Admin commands run the same ops in Debug mode.
