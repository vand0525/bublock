# DraftPlugin

Thin plugin class (Name `Rift Roulette Draft`) for the hero draft. Ops live on
`DraftService`; this class passes its own `Timer` to ops that wait a tick.
Extracted from Legacy in Stage 9.

## Hooks (Clean mode)

| Hook | Does |
|---|---|
| `OnLoad(isReload: true)` | Next tick: `DraftService.RedrawBoards()` (a hot reload skips `OnStartupServer`; clears the old DLL's boards and draws them at the current watch spot) |
| `OnStartupServer` | Next tick: `DraftService.RedrawBoards()` (archive startup board draw) |
| `player_hero_changed` | Archive null check (pawn and its controller present), then `DraftService.EnforceHero(controller, pawn, Timer)` (Random mode hero guard first) |

## Player commands (in game, Clean)

Each sends the op's result to the caller with `PlayerChat.Send`.

| Command | Archive name | Does |
|---|---|---|
| `/pick <hero>` | `/select` | `DraftService.Pick` |
| `/unpick` | `/unselect` | `DraftService.Unpick` |
| `/picks` | `/selected` | `DraftService.DescribePicks` (hero plus picker name and slot) |
| `/heroes` | — | `DraftService.DescribeHeroes` (two pool lines; one "heroes are random" line in Random mode) |

Archive names were removed in Stage 12 (no aliases). In Random mode
(Stage 13b) `/pick`, `/unpick`, `/draft_assign`, and `/draft_release` reply
with `DraftService.RandomModeReply` and change nothing.

## Admin commands (Debug)

`AdminCommand.Authorize` first (server console trusted; calls logged in
`Draft`), then the op in Debug mode; results via `AdminCommand.Reply`.

| Command | Archive name | Does |
|---|---|---|
| `/draft_status` | — | `DescribeDraft` lines |
| `/draft_assign <slot> <hero>` | — | `Pick` for that player (same rules as `/pick`); the player also gets the chat line |
| `/draft_release <slot>` | — | `Unpick` for that player (same rules as `/unpick`); the player also gets the chat line |
| `/draft_reset` | `/reset` | `Reset`; replies with the number of players returned |
| `/draft_boards` | — | `RedrawBoards(Debug)` (Random mode: welcome + stats boards) |

The archive `/reset` was ungated; `/draft_reset` is admin-only
(intentional difference).

## Invariants

- `player_hero_changed` calls `SelfTest/EventCounters.Hit("player_hero_changed")` first (self-test hook check).

- No state of its own; picks live in `DraftState`.
- Hooks never throw.
