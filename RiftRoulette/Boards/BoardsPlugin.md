# BoardsPlugin

Thin plugin class (Name `Rift Roulette Boards`) for the boards at the
watch spot. Ops live on `BoardService` and `WelcomeNoteStore`.

## Hooks (Clean mode)

| Hook | Does |
|---|---|
| `OnLoad(isReload: true)` | Next tick: `BoardService.Redraw()` (a hot reload skips `OnStartupServer`; clears the old DLL's boards and draws them at the current watch spot) |
| `OnStartupServer` | Next tick: `BoardService.Redraw()` |

## Admin commands (Debug)

`AdminCommand.Authorize` first (server console trusted; calls logged in
`Boards`), then the op in Debug mode; results via `AdminCommand.Reply`.

| Command | Does |
|---|---|
| `/board_redraw` | `BoardService.Redraw(Debug)`; replies `[Boards] Boards redrawn` |
| `/board_note [text]` | `WelcomeNoteStore.Set` (args joined with spaces, `\n` is a line break; no text clears it), then `BoardService.Redraw(Debug)`; replies with a preview or `Note cleared` |

## Invariants

- No state of its own.
- Hooks never throw.
