using System.Globalization;

namespace Bublock.Modules.Session;

public enum SessionPhase
{
  Waiting,
  Playing,
  Break
}

public enum SessionAction
{
  None,
  Start,
  Stop
}

public sealed record SessionOptions(int MatchSeconds = 120, int BreakSeconds = 5, int MinPlayers = 2, int WarningSeconds = 10)
{
  public const int MinMatchSeconds = 30;
  public const int MaxMatchSeconds = 1800;
}

public sealed record MatchResult(int Match, IReadOnlyList<(ulong SteamId, int Points)> Standings, IReadOnlyList<ulong> Winners);

public static class SessionRule
{
  // Waiting starts once enough players are on; a running match or break stops when too few remain.
  public static SessionAction Decide(SessionPhase phase, int players, int minPlayers) =>
    phase == SessionPhase.Waiting
      ? players >= minPlayers ? SessionAction.Start : SessionAction.None
      : players < minPlayers ? SessionAction.Stop : SessionAction.None;

  public static bool IsValidMatchSeconds(int seconds) =>
    seconds is >= SessionOptions.MinMatchSeconds and <= SessionOptions.MaxMatchSeconds;

  // Everyone tied on the top score wins; nobody wins with no points.
  public static IReadOnlyList<ulong> Winners(IReadOnlyList<(ulong SteamId, int Points)> standings)
  {
    if (standings.Count == 0 || standings[0].Points <= 0)
      return [];

    var top = standings[0].Points;
    return standings.Where(row => row.Points == top).Select(row => row.SteamId).ToList();
  }

  public static string Clock(int seconds)
  {
    var clamped = Math.Max(0, seconds);
    return $"{clamped / 60}:{(clamped % 60).ToString("00", CultureInfo.InvariantCulture)}";
  }
}
