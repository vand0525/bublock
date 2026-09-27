# WorldTextFormatTests

Unit tests for `Modules/WorldText/WorldTextFormat`.

- `FromArgs` joins arguments with single spaces, turns a typed `\n` into a
  line break, and returns an empty string for no arguments.
- `Preview` keeps short text, flattens line breaks to ` / `, and truncates
  long text to the limit plus `...`.
