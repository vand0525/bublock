namespace RiftRoulette.Lobby;

public enum ParkAction
{
  Stay,
  Park,
  Repark
}

public static class StreamCamRule
{
  // parkedForSide: the last park was for this side and nothing was followed since; placed: the fly cam park landed;
  // handled: the viewer moved the camera after it landed; moved: the viewer is moving it right now.
  public static ParkAction ParkStep(
    bool flyCam,
    bool parkedForSide,
    bool placed,
    bool handled,
    bool moved,
    TimeSpan? sinceLastPark,
    TimeSpan reparkEvery)
  {
    var due = sinceLastPark is not { } since || since >= reparkEvery;

    if (!flyCam)
      return !parkedForSide ? ParkAction.Park : due ? ParkAction.Repark : ParkAction.Stay;

    // Never move the camera while the viewer flies it: that crashed the client.
    if (moved)
      return ParkAction.Stay;

    if (!parkedForSide)
      return ParkAction.Park;

    if (placed || handled)
      return ParkAction.Stay;

    return due ? ParkAction.Repark : ParkAction.Stay;
  }
}
