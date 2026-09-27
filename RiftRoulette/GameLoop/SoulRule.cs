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

  public static bool ShouldBlock(
    ECurrencyType type,
    ECurrencySource source,
    int amount,
    bool matchRunning,
    bool ranksFromBuild = false)
  {
    if (!matchRunning || amount <= 0)
      return false;

    return type switch
    {
      ECurrencyType.EGold => !AllowedSources.Contains(source),
      ECurrencyType.EAbilityPoints or ECurrencyType.EAbilityUnlocks => ranksFromBuild && source != ECurrencySource.ECheats,
      _ => false
    };
  }
}
