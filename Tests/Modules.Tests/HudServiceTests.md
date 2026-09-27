# HudServiceTests

Unit tests for `Modules/Hud/HudService.ParseAnnouncement` (the only pure
part of the Hud module).

- No `|`: the whole trimmed text is the title, description empty.
- Splits on the first `|` only; both parts trimmed.
- A lone `|` gives an empty title (the command rejects it).
