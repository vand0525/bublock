namespace RiftRoulette.Lobby;

public static class MapRefreshRule
{
  // Rough estimate from the 2026-09-28 failed joins (first failure at 284 fighter-rounds,
  // ~1.15 KB each, 512 KB near 230). Do not raise without new join-size data.
  public const int DefaultBudget = 160;

  public static bool Due(int fighterRounds, int budget) =>
    budget > 0 && fighterRounds >= budget;

  // Null when the refresh is off or there is no team size to count with.
  public static int? RoundsLeft(int fighterRounds, int budget, int fighters)
  {
    if (budget <= 0 || fighters <= 0)
      return null;

    var left = budget - fighterRounds;
    return left <= 0 ? 0 : (left + fighters - 1) / fighters;
  }
}
