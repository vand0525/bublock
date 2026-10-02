# MatchConfigTests

Unit tests for the pure parts of `GameLoop/MatchConfig`.

- `TryParseHeroMode` accepts `random` / `mirror` in any case, with
  surrounding spaces trimmed; rejects empty, numeric (`0`, `1`, `-1`),
  unknown names, other digit forms such as `2v2`, and the removed modes
  `draft`, `duel` and `1v1`.
- `TryParseFormat` accepts `continuous` only.
- `Names<HeroMode>()` is `random|mirror`; `Describe` uses lowercase values.
- Never calls the static setters, so tests stay independent.
