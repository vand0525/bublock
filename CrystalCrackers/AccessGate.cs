using Bublock.Modules.Access;
using Bublock.Shared;
using DeadworksManaged.Api;

namespace CrystalCrackers;

public static class AccessGate
{
  public const int LoadKickSeconds = 2;

  private static readonly Logger Log = BublockLog.For("Access");

  public static AccessVerdict Check(ulong steamId) =>
    AccessStore.Load().Check(steamId, AdminAuth.IsAuthorized(steamId), forcePrivate: true);

  public static bool AllowConnect(ulong steamId, string name)
  {
    if (steamId == 0)
      return true;

    var verdict = Check(steamId);

    if (verdict == AccessVerdict.Allowed)
      return true;

    Log.Warn("Connection refused Verdict={Verdict} Name={Name} SteamId={SteamId}", verdict, name, steamId);
    return false;
  }

  public static IReadOnlyList<string> KickWithoutAccess(ExecutionMode mode = ExecutionMode.Clean)
  {
    var kicked = new List<string>();

    foreach (var player in Players.GetAll().Where(player => !player.IsBot && player.PlayerSteamId != 0).ToList())
    {
      var verdict = Check(player.PlayerSteamId);

      if (verdict == AccessVerdict.Allowed)
        continue;

      Log.WithMode(mode).Warn(player.ToPlayerRef(), "Kicking player without access Verdict={Verdict}", verdict);
      Server.ExecuteCommand($"kickid {player.Slot}");
      kicked.Add(player.PlayerName);
    }

    return kicked;
  }
}
