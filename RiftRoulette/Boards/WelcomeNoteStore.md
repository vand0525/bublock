# WelcomeNoteStore

Keeps the note drawn under the welcome board, below the fixed `/about`
hint (`board.note`), in
`bublock/welcomenote.txt` on the server (next to `bublock/logs/` and
`access.json`), so it survives redraws, uploads (hot reloads) and restarts.

## Operations

| Operation | Effect |
|---|---|
| `FilePath` | `<bublock>/welcomenote.txt`, from `LogPaths.ResolveRoot()`. |
| `Text` | The note (trimmed), read from the file on first use; missing file: empty. |
| `Set(text, mode)` | Sets the note (trimmed; empty clears it) and rewrites the file. |

## Side effects

- Reads the file once per load; writes it on every set.
- Logs (`Boards` feature log, `boards-YYYYMMDD.log`) Information
  `Welcome note loaded Length=` on load, Debug on write;
  an unreadable file logs Error and draws no note; a failed write logs
  Error and keeps the note for this load only.

## Invariants

- The in-memory note is the truth for this load; the file only seeds the
  next load.
- Plain text; a newline in the file is a line break on the board. A hand
  edit applies after the next load.
- Does not redraw; callers redraw through `BoardService.Redraw`.
