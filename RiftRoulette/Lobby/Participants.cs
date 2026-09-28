using DeadworksManaged.Api;

namespace RiftRoulette.Lobby;

public static class Participants
{
  public static List<CCitadelPlayerController> Humans() =>
    Players.GetAll()
      .Where(IsParticipant)
      .ToList();

  public static bool IsParticipant(CCitadelPlayerController player) =>
    !player.IsBot && !AdminSeat.IsSeated(player.PlayerSteamId) && !BanStatueService.IsStatue(player.PlayerSteamId);
}
