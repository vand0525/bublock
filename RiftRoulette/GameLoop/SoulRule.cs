using DeadworksManaged.Api;

namespace RiftRoulette.GameLoop;

public static class SoulRule
{
  private static readonly HashSet<ECurrencySource> AllowedSources =
  [
    ECurrencySource.ECheats,
    ECurrencySource.EStartingAmount,
    ECurrencySource.EItemSale
  ];

  public static bool ShouldBlock(ECurrencyType type, ECurrencySource source, int amount, bool matchRunning) =>
    matchRunning
    && type == ECurrencyType.EGold
    && amount > 0
    && !AllowedSources.Contains(source);
}
