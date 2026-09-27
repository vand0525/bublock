using DeadworksManaged.Api;

namespace Bublock.Modules.Loadout;

public sealed record SnapshotAbility(string Name, EAbilitySlot Slot, int UpgradeBits);

public sealed record SnapshotItem(string Name, IReadOnlyList<string> ImbuedAbilities);

public sealed record LoadoutSnapshot(
  Heroes Hero,
  int Level,
  int AbilityPoints,
  int AbilityUnlocks,
  IReadOnlyList<SnapshotAbility> Abilities,
  IReadOnlyList<SnapshotItem> Items,
  string Source)
{
  public string Describe() =>
    $"Hero={Hero} | Level={Level} | AP={AbilityPoints} | Unlocks={AbilityUnlocks} | " +
    $"Abilities={string.Join(",", Abilities.Select(ability => $"{ability.Slot}:{Convert.ToString(ability.UpgradeBits, 2)}"))} | " +
    $"Items={Items.Count} | From={Source}";
}

public sealed record SnapshotResult(int ItemsAdded, int ItemsFailed, int Imbued, int AbilitiesSet, int AbilitiesMissing);
