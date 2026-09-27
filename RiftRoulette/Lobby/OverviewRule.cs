namespace RiftRoulette.Lobby;

public static class OverviewRule
{
  public static readonly TimeSpan Duration = TimeSpan.FromSeconds(10);

  public static DateTime EndFrom(DateTime now) => now + Duration;

  public static bool Showing(DateTime? end, DateTime now) => end is { } until && now < until;

  public static bool CanStart(bool roundLive, int roundNumber, int? lastShownRound) =>
    roundLive && roundNumber > 0 && roundNumber != lastShownRound;
}
