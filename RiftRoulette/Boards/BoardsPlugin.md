# BoardsPlugin

Thin plugin class (Name `Rift Roulette Boards`) for the boards at the
watch spot and the empty-server sign. Ops live on `BoardService`,
`EmptySign` and `WelcomeNoteStore`.

## Hooks (Clean mode)

| Hook | Does |
|---|---|
| `OnLoad(isReload: true)` | Next tick: `BoardService.Redraw()` (a hot reload skips `OnStartupServer`; clears the old DLL's boards and draws them at the current watch spot) |
| `OnLoad` (every load) | `Timer.Every(EmptySign.CheckSeconds)` (2 s): `EmptySign.Refresh()` shows the empty-server sign while there are no participants and removes it otherwise |
| `OnStartupServer` | Next tick: `BoardService.Redraw()` |

## Admin commands (Debug)

`AdminCommand.Authorize` first (server console trusted; calls logged in
`Boards`), then the op in Debug mode; results via `AdminCommand.Reply`.

| Command | Does |
|---|---|
| `/board_redraw` | `BoardService.Redraw(Debug)`; replies `[Boards] Boards redrawn` |
| `/board_note [text]` | `WelcomeNoteStore.Set` (args joined with spaces, `\n` is a line break; no text clears it), then `BoardService.Redraw(Debug)`; replies with a preview or `Note cleared` |
| `/sign_status` | Replies `Sign Shown= Preview=` and `EmptySignStore.Current.Describe()` |
| `/sign_distance <units>` | `EmptySign.Change`: `Distance` |
| `/sign_shift <units>` | `EmptySign.Change`: `Shift` (negative: left) |
| `/sign_height <units>` | `EmptySign.Change`: `Height` (negative: down) |
| `/sign_size <title> [join]` | `EmptySign.Change`: `TitleScale`, and `JoinScale` when given |
| `/sign_gap <units>` | `EmptySign.Change`: `Gap` |
| `/sign_color <title\|join\|both> <r> <g> <b>` | `EmptySign.Change`: `TitleColor` / `JoinColor` / both |
| `/sign_text <title\|join> [text]` | `EmptySign.Change`: `TitleText` / `JoinText` (args joined with spaces; no text restores the default) |
| `/sign_preview <on\|off>` | `EmptySign.SetPreview`; replies with the new state and `Shown=` |
| `/sign_reset` | `EmptySign.Change` to `EmptySignSettings.Default` |

Every `/sign_*` change goes through `ChangeSign`: authorize, `EmptySign.Change(Debug)`; an out-of-range value or a bad line name is a `CommandException` with the reason (nothing saved). Success replies `Sign saved` (noting when it is not up because players are on) and the full settings line.

## Invariants

- No state of its own.
- Hooks never throw.
