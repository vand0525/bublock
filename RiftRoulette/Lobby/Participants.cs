using DeadworksManaged.Api;

namespace RiftRoulette.Lobby;

public static class Participants
{
  // The disconnect hook still lists the leaving controller; acting on it (hero swap, send up) fails.
  private static readonly HashSet<ulong> Leaving = [];

  public static List<CCitadelPlayerController> Humans() =>
    Players.GetAll()
      .Where(IsParticipant)
      .ToList();

  public static bool IsParticipant(CCitadelPlayerController player) =>
    !player.IsBot &&
    !Leaving.Contains(player.PlayerSteamId) &&
    !AdminSeat.IsSeated(player.PlayerSteamId) &&
    !BanStatueService.IsStatue(player.PlayerSteamId);

  public static void MarkLeaving(ulong steamId) => Leaving.Add(steamId);

  public static void ClearLeaving(ulong steamId) => Leaving.Remove(steamId);
}
