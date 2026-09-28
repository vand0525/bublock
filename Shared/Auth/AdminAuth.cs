namespace Bublock.Shared;

public static class AdminAuth
{
  private static readonly HashSet<ulong> AuthorizedSteamIds =
  [
    76561198192980843, // Theo (archive DevTools oracle)
    76561198001002148, // server owner (given by the owner, 2026-09-27)
  ];

  public static bool IsAuthorized(ulong steamId) =>
    AuthorizedSteamIds.Contains(steamId);

  public static IReadOnlyCollection<ulong> SteamIds => AuthorizedSteamIds;
}
