# SpectateRuleTests

Unit tests for `Modules/Spectate/SpectateRule`.

- `Choose` keeps a live current target (even when a killer is given).
- Picks the killer when the current target is no longer a candidate.
- Picks the first candidate (`Any`) when there is no killer, or the killer
  is not a candidate.
- Never picks the dead victim (a suicide's killer is the victim, not a
  candidate).
- Parks when there are no candidates.
- `LookDown` returns pitch 89, the given yaw and roll 0.
- `ParkCheck` is true only when roaming, with no target, within the
  tolerance (the edge counts); not roaming, a target, or too far fail.
- `IsManualMove` is true only when roaming, with no target, past the
  tolerance (the edge does not count).
- `ManualActive` is true before the hold's end, false at it and with no hold.
- `FollowReady` is true with no spawn seen and from the grace on; false
  inside it.
