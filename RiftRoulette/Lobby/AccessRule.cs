namespace RiftRoulette.Lobby;

public enum AccessVerdict
{
  Allowed,
  Banned,
  Private
}

public static class AccessRule
{
  public const ulong SteamIdBase = 76561197960265728;

  public static AccessVerdict Check(bool banned, bool privateMode, bool allowed, bool isAdmin) =>
    banned ? AccessVerdict.Banned
    : privateMode && !allowed && !isAdmin ? AccessVerdict.Private
    : AccessVerdict.Allowed;

  public static bool IsSteamId(ulong steamId) =>
    steamId > SteamIdBase && steamId - SteamIdBase <= uint.MaxValue;

  public static bool TryParseSteamId(string text, out ulong steamId) =>
    ulong.TryParse(text.Trim(), out steamId) && IsSteamId(steamId);
}
