# DraftPlugin

Thin plugin class (Name `Rift Roulette Draft`) for the hero draft. Ops live on
`DraftService`; this class passes its own `Timer` to ops that wait a tick.

## Hooks (Clean mode)

| Hook | Does |
|---|---|
| `OnLoad(isReload: true)` | Next tick: `DraftService.RedrawBoards()` (a hot reload skips `OnStartupServer`; clears the old DLL's boards and draws them at the current watch spot) |
| `OnStartupServer` | Next tick: `DraftService.RedrawBoards()` |
| `player_hero_changed` | Null check (pawn and its controller present), then `DraftService.EnforceHero(controller, pawn, Timer)` (Random mode hero guard first) |

## Player commands (in game, Clean)

Each sends the op's result to the caller with `PlayerChat.Send`.

| Command | Does |
|---|---|
| `/pick <hero>` | `DraftService.Pick` |
| `/unpick` | `DraftService.Unpick` |
| `/picks` | `DraftService.DescribePicks` (hero plus picker name and slot) |
| `/heroes` | `DraftService.DescribeHeroes` (two pool lines; one "heroes are random" line in Random mode) |

In Random mode `/pick`, `/unpick`, `/draft_assign`, and `/draft_release`
reply with `DraftService.RandomModeReply` and change nothing.

## Admin commands (Debug)

`AdminCommand.Authorize` first (server console trusted; calls logged in
`Draft`), then the op in Debug mode; results via `AdminCommand.Reply`.

| Command | Does |
|---|---|
| `/draft_status` | `DescribeDraft` lines |
| `/draft_assign <slot> <hero>` | `Pick` for that player (same rules as `/pick`); the player also gets the chat line |
| `/draft_release <slot>` | `Unpick` for that player (same rules as `/unpick`); the player also gets the chat line |
| `/draft_reset` | `Reset`; replies with the number of players returned |
| `/draft_boards` | `RedrawBoards(Debug)` (Random mode: welcome + stats boards) |
| `/draft_note [text]` | `WelcomeNoteStore.Set` (args joined with spaces, `\n` is a line break; no text clears it), then `RedrawBoards(Debug)`; replies with a preview or "Note cleared" |

`/draft_reset` is admin-only.

## Invariants

- `player_hero_changed` calls `SelfTest/EventCounters.Hit("player_hero_changed")` first (self-test hook check).

- No state of its own; picks live in `DraftState`.
- Hooks never throw.
