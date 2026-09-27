# LogFormatter

Pure text formatting for log lines. No I/O, no Deadworks types.

## Format

```text
2026-09-26T20:13:00.123Z [Information] [RiftRoulette.Draft] session=a1b2c3d4 round=- player="Theo" steam=76561198192980843 slot=3 | Selected hero Hero=Shiv
```

- Timestamp: ISO-8601 UTC with milliseconds and `Z`.
- `[Level]`: `LogLevel` name.
- `[Prefix]`: `<Dll>` for master, `<Dll>.<Feature>` for feature loggers.
- `session=` always; `round=` is `-` when no round is active.
- Player fields only when a `PlayerRef` is given (`"` in names becomes `'`).
- Exceptions (`Error` / `Critical`) are appended on following lines with
  full stack (`Exception.ToString()`).

## RenderTemplate

- Each `{Name}` placeholder is replaced, in order, with `Name=value`, so
  values stay greppable (`Hero=Shiv`).
- When the template already writes `Name=` right before the placeholder
  (`Hero={Hero}`), only the value is appended, so the line reads `Hero=Shiv`,
  not `Hero=Hero=Shiv`. The name must match exactly and start a word.
- `{{` and `}}` produce literal braces.
- Missing arguments leave the placeholder as `{Name}`; extra arguments are
  ignored.
- Values: `null` → `null`; `IFormattable` uses invariant culture; strings
  that are empty or contain whitespace are quoted.
