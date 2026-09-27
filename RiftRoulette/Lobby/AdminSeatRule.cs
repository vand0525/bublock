namespace RiftRoulette.Lobby;

public static class AdminSeatRule
{
  public const int PlayerCap = 12;

  public static bool CanConnect(bool isAdmin, int playing, int cap = PlayerCap) =>
    isAdmin || playing < cap;

  public static bool CanStand(int playing, int cap = PlayerCap) =>
    playing < cap;

  public static bool SeatOnJoin(bool isAdmin) => isAdmin;
}
