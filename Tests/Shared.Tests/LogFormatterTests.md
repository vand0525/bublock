# LogFormatterTests

Checks `LogFormatter` output exactly:

- UTC ISO timestamp, `[Level]`, `[Prefix]`, `session=`, `round=-`.
- Local input times are converted to UTC.
- Player fields (`player=`, `steam=`, `slot=`) and round id; quotes in names
  become `'`.
- Exception stack appended after the message.
- Template rendering: `{Name}` → `Name=value`, quoting, `null`, invariant
  culture, missing placeholders, `{{ }}` escapes.
