namespace Bublock.Modules.Loadout;

public static class ItemSlots
{
  public const int BaseSlots = 9;

  public const int MaxSlots = 12;

  public static readonly IReadOnlyList<(int Souls, int Slots)> Breakpoints =
  [
    (16000, 10),
    (22000, 11),
    (28000, 12)
  ];

  public static int ForSouls(int souls)
  {
    var slots = BaseSlots;

    foreach (var (threshold, count) in Breakpoints)
    {
      if (souls < threshold)
        break;

      slots = count;
    }

    return slots;
  }

  public static bool ExtraPasses(int slots) => slots >= MaxSlots;

  public static string Describe() =>
    $"{BaseSlots} items, then " +
    string.Join(", ", Breakpoints.Select(entry => $"{entry.Slots} from {entry.Souls.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)}"));
}
