# MatchConfigTests

Unit tests for the pure parts of `GameLoop/MatchConfig`.

- `TryParseHeroMode` accepts `random` / `draft` / `duel` / `mirror` in any
  case, with surrounding spaces trimmed, and the alias `1v1` (any case) for `duel`;
  rejects empty, numeric (`0`, `1`, `-1`), unknown names, and other digit
  forms such as `2v2`.
- `TryParseFormat` accepts `continuous` only.
- `Names<HeroMode>()` is `random|draft|duel|mirror`; `Describe` uses lowercase values.
- Never calls the static setters, so tests stay independent.
