using DeadworksManaged.Api;

namespace Bublock.Modules.Economy;

public static class SoulRule
{
  // Gold from these sources still lands: cheats (loadouts set gold this way), the starting amount, item sales.
  private static readonly HashSet<ECurrencySource> AllowedSources =
  [
    ECurrencySource.ECheats,
    ECurrencySource.EStartingAmount,
    ECurrencySource.EItemSale
  ];

  // For game types where power comes only from builds: earned souls (kills, orbs, income) are blocked,
  // and with ranksFromBuild ability points and unlocks only arrive through cheats (the loadout).
  public static bool ShouldBlock(ECurrencyType type, ECurrencySource source, int amount, bool active, bool ranksFromBuild = true)
  {
    if (!active || amount <= 0)
      return false;

    return type switch
    {
      ECurrencyType.EGold => !AllowedSources.Contains(source),
      ECurrencyType.EAbilityPoints or ECurrencyType.EAbilityUnlocks => ranksFromBuild && source != ECurrencySource.ECheats,
      _ => false
    };
  }
}
