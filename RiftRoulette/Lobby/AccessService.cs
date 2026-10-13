using Bublock.Modules.Access;
using Bublock.Shared;
using DeadworksManaged.Api;
using ITimer = DeadworksManaged.Api.ITimer;

namespace RiftRoulette.Lobby;

public static class AccessService
{
  private static readonly Logger Log = BublockLog.For("Access");

  public static string FilePath => AccessStore.FilePath;

  public static AccessList Load() => AccessStore.Load();

  public static bool AllowConnect(ulong steamId, string name)
  {
    if (steamId == 0)
      return true;

    var verdict = Load().Check(steamId, AdminAuth.IsAuthorized(steamId));

    switch (verdict)
    {
      case AccessVerdict.Banned:
        return BanStatueService.AdmitBanned(steamId, name);

      case AccessVerdict.Private:
        Log.Warn("Connection refused, server is private Name={Name} SteamId={SteamId}", name, steamId);
        return false;

      default:
        return true;
    }
  }

  public static AccessVerdict Check(CCitadelPlayerController player) =>
    Load().Check(player.PlayerSteamId, AdminAuth.IsAuthorized(player.PlayerSteamId));

  public static string Ban(ulong steamId, ExecutionMode mode = ExecutionMode.Clean) => AccessStore.Ban(steamId, mode);

  public static string Unban(ulong steamId, ExecutionMode mode = ExecutionMode.Clean) => AccessStore.Unban(steamId, mode);

  public static string Allow(ulong steamId, ExecutionMode mode = ExecutionMode.Clean) => AccessStore.Allow(steamId, mode);

  public static string Disallow(ulong steamId, ExecutionMode mode = ExecutionMode.Clean) => AccessStore.Disallow(steamId, mode);

  public static string SetPrivate(bool privateMode, ExecutionMode mode = ExecutionMode.Clean) =>
    AccessStore.SetPrivate(privateMode, mode);

  public static string SetStatueModifier(string? name, ExecutionMode mode = ExecutionMode.Clean) =>
    AccessStore.SetStatueModifier(name, mode);

  // Banned players still in the game become statues and are kicked after LiveBanKickSeconds.
  public static IReadOnlyList<string> PetrifyBanned(ITimer timer, ExecutionMode mode = ExecutionMode.Clean)
  {
    var list = Load();
    var petrified = new List<string>();

    foreach (var player in Players.GetAll().Where(player => !player.IsBot && list.Banned.Contains(player.PlayerSteamId)).ToList())
    {
      if (BanStatueService.IsStatue(player.PlayerSteamId))
        continue;

      BanStatueService.Petrify(player, BanStatueService.LiveBanKickSeconds, liveBan: true, timer, mode);
      petrified.Add(player.PlayerName);
    }

    return petrified;
  }

  // Banned players are left to PetrifyBanned; this kicks only players shut out by private mode.
  public static IReadOnlyList<string> KickDenied(ExecutionMode mode = ExecutionMode.Clean)
  {
    var list = Load();
    var kicked = new List<string>();

    foreach (var player in Players.GetAll().Where(player => !player.IsBot && player.PlayerSteamId != 0).ToList())
    {
      var verdict = list.Check(player.PlayerSteamId, AdminAuth.IsAuthorized(player.PlayerSteamId));

      if (verdict != AccessVerdict.Private)
        continue;

      Log.WithMode(mode).Info(player.ToPlayerRef(), "Kicking player without access Verdict={Verdict}", verdict);

      if (LobbyService.KickPlayer(player.Slot, mode))
        kicked.Add(player.PlayerName);
    }

    return kicked;
  }

  public static IReadOnlyList<string> Describe() => AccessStore.Describe();

  public static IReadOnlyList<string> DescribeBanned() =>
    AccessStore.DescribeIds("Banned", Load().Banned, steamId => $" | {BanStatueService.Describe(steamId)}");

  public static IReadOnlyList<string> DescribeAllowed() => AccessStore.DescribeIds("Allowed", Load().Allowed);
}
