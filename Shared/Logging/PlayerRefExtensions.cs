using DeadworksManaged.Api;

namespace Bublock.Shared;

public static class PlayerRefExtensions
{
  public static PlayerRef ToPlayerRef(this CBasePlayerController player) =>
    new(player.Slot, player.PlayerSteamId, player.PlayerName);
}
