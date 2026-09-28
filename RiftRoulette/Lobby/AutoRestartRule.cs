namespace RiftRoulette.Lobby;

public static class AutoRestartRule
{
  public const string StuckJoin = "stuck-join";
  public const string Uptime = "uptime";

  public const double MinUptimeSeconds = 10 * 60;
  public const double MaxUptimeSeconds = 3 * 60 * 60;
  public const double StuckJoinSeconds = 3 * 60;

  // Null means stay up. Only ever with nobody playing, never within MinUptimeSeconds of the
  // last map start (a reload that does not clear stuck joins must not loop).
  public static string? Reason(bool enabled, int participants, int stuckJoins, int joinsInProgress, double uptimeSeconds)
  {
    if (!enabled || participants > 0 || uptimeSeconds < MinUptimeSeconds)
      return null;

    if (stuckJoins > 0)
      return StuckJoin;

    if (uptimeSeconds >= MaxUptimeSeconds && joinsInProgress == 0)
      return Uptime;

    return null;
  }

  public static bool IsStuck(double pendingSeconds) => pendingSeconds >= StuckJoinSeconds;
}
