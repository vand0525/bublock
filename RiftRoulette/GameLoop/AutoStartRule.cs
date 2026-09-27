namespace RiftRoulette.GameLoop;

public enum AutoStartAction
{
  None,
  Start,
  End
}

public static class AutoStartRule
{
  public const int DefaultMinPlayers = 2;

  public static AutoStartAction Decide(
    bool enabled,
    bool running,
    int humans,
    int minPlayers = DefaultMinPlayers,
    bool leaving = false)
  {
    if (!enabled)
      return AutoStartAction.None;

    if (!running && humans >= minPlayers && !leaving)
      return AutoStartAction.Start;

    if (running && humans < minPlayers)
      return AutoStartAction.End;

    return AutoStartAction.None;
  }
}
