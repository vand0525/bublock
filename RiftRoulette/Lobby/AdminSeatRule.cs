namespace RiftRoulette.Lobby;

public enum AdminMode
{
  Play,
  Spectate,
  Roam
}

public static class AdminSeatRule
{
  public const int PlayerCap = 12;

  public static bool CanConnect(bool isAdmin, int playing, int cap = PlayerCap) =>
    isAdmin || playing < cap;

  public static bool CanStand(int playing, int cap = PlayerCap) =>
    playing < cap;

  public static AdminMode JoinMode(bool isAdmin, int othersPlaying, AdminMode? last, int cap = PlayerCap)
  {
    if (!isAdmin)
      return AdminMode.Play;

    var mode = last ?? AdminMode.Play;
    return mode == AdminMode.Play && othersPlaying >= cap ? AdminMode.Spectate : mode;
  }

  public static bool ShouldRoam(int participants) => participants == 0;
}
