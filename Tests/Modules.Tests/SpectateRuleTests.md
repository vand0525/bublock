# SpectateRuleTests

Unit tests for `Modules/Spectate/SpectateRule`.

- `Choose` keeps a live current target (even when a killer is given).
- Picks the killer when the current target is no longer a candidate.
- Picks the first candidate (`Any`) when there is no killer, or the killer
  is not a candidate.
- Never picks the dead victim (a suicide's killer is the victim, not a
  candidate).
- Parks when there are no candidates.
- `FollowReady` is true with no spawn seen and from the grace on; false
  inside it.
- `LookDown` returns pitch 89, the given yaw and roll 0.
- `WrapDegrees` maps into [-180, 180) (190 is -170, 180 is -180, 360 is 0).
- `Turned` wraps yaw (179 to -179 is 2) and takes the larger of pitch and
  yaw.
- `HandMoved` is true past 50 units or 3 degrees (the edges do not count).
