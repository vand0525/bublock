namespace Bublock.Shared;

public static class AdminAuth
{
  private static readonly HashSet<ulong> AuthorizedSteamIds =
  [
    76561198192980843
  ];

  public static bool IsAuthorized(ulong steamId) =>
    AuthorizedSteamIds.Contains(steamId);

  public static IReadOnlyCollection<ulong> SteamIds => AuthorizedSteamIds;
}
