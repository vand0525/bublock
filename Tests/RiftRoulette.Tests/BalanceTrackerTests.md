# BalanceTrackerTests

Unit tests for `Balance/BalanceTracker`.

- Stomp: a 2-round lead plus 10 vs 2 kills.
- No stomp with a 3-round lead but close kills (20 vs 14), or with a big
  kill gap but no round lead.
- A 5-round streak triggers; non-scoring rounds don't break it; a win by the
  other team restarts it.
- `Reset` clears the counters; `Leader` uses rounds, then kills.
