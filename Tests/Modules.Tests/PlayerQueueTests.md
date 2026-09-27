# PlayerQueueTests

Unit tests for `Modules/Queue/PlayerQueue`.

- `Join` appends and returns the 1-based position; joining again keeps the
  original place (no duplicates).
- `Leave` closes the gap; `PositionOf` / `Contains` reflect it.
- `Front(n)` returns at most `n`, none for 0.
- `MoveToBack` moves a queued ID to the end; false for an unknown ID.
- `RemoveWhere` removes matches and returns the count; `Clear` empties.
