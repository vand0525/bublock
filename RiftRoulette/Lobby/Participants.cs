using DeadworksManaged.Api;

namespace RiftRoulette.Lobby;

public static class Participants
{
  public static List<CCitadelPlayerController> Humans() =>
    Players.GetAll()
      .Where(player => !player.IsBot && !AdminSeat.IsSeated(player.PlayerSteamId))
      .ToList();

  public static bool IsParticipant(CCitadelPlayerController player) =>
    !player.IsBot && !AdminSeat.IsSeated(player.PlayerSteamId);
}
