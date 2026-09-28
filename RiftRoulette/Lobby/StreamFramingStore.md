# StreamFramingStore

Keeps the stream camera's saved framing per rift side in
`bublock/streamcam.json` on the server (next to `bublock/logs/` and
`access.json`), so it survives uploads (hot reloads) and restarts.

## Operations

| Operation | Effect |
|---|---|
| `FilePath` | `<bublock>/streamcam.json`, from `LogPaths.ResolveRoot()`. |
| `All` | The saved poses, read from the file on first use (missing file: none). |
| `For(side)` | `StreamFraming.Pick(All, side)`: the side's pose, the other side's, or the default. |
| `Save(side, pose, mode)` | Sets the side's pose and rewrites the file. |
| `Reset(mode)` | Clears every pose and rewrites the file (empty object). |

## Side effects

- Reads the file once per load; writes it on every save / reset.
- Logs Information `Stream camera framing loaded Sides=` on load, Debug
  on write; an unreadable file logs Error and uses the default; a failed
  write logs Error and keeps the poses for this load only.

## Invariants

- The in-memory poses are the truth for this load; the file only seeds the
  next load.
- The file is written by the server only; a hand edit applies after the
  next load.
