namespace Bublock.Modules.Loadout;

public sealed record AbilityPlan(
  IReadOnlyList<(string Ability, int Bits)> Bits,
  int StepsTaken,
  int StepsTotal,
  int UnlocksUsed,
  int PointsUsed);

public static class LoadoutPlanner
{
  public const int DefaultSlots = 9;

  public const int MaxUpgrades = 3;

  public static readonly IReadOnlyList<int> UpgradeCosts = [1, 2, 5];

  public const int FullyUpgradedBits = 0b11111;

  public const int DefaultMinItems = 6;

  public const int DefaultCap = 20000;
  public const int MinCap = 1000;
  public const int MaxCap = 200000;

  public const string DefaultCapWord = "default";

  public static readonly IReadOnlySet<string> Banned = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
  {
    "upgrade_non_player_bonus",
    "upgrade_non_player_bonus_sacrifice",
    "upgrade_goose_egg",
    "upgrade_trophy_collector"
  };

  public static IReadOnlyList<string> ItemOrder(
    IReadOnlyList<BuildCategory>? categories,
    IReadOnlyList<string>? fallback,
    Random? rng = null)
  {
    var order = new List<string>();

    if (categories == null || categories.Count == 0)
    {
      foreach (var item in fallback ?? [])
      {
        if (!Banned.Contains(item) && !order.Contains(item))
          order.Add(item);
      }

      return order;
    }

    foreach (var category in categories)
    {
      var items = (category.Items ?? []).Where(item => !Banned.Contains(item) && !order.Contains(item)).Distinct().ToList();

      if (items.Count == 0)
        continue;

      if (category.Optional)
        order.Add(rng == null ? items[0] : items[rng.Next(items.Count)]);
      else
        order.AddRange(items);
    }

    return order;
  }

  public static IReadOnlyList<string> ItemOrder(HeroBuild build, Random? rng = null) =>
    ItemOrder(build.Categories, build.Items, rng);

  public static IReadOnlyList<string> FirstSlots(
    IEnumerable<string> order,
    Func<string, IReadOnlyList<string>> componentsOf,
    int slots = DefaultSlots,
    Func<string, bool>? exists = null)
  {
    var owned = new List<string>();

    foreach (var item in order)
    {
      if (owned.Count >= slots)
        break;

      if (owned.Contains(item) || Banned.Contains(item))
        continue;

      if (exists != null && !exists(item))
        continue;

      foreach (var component in componentsOf(item))
        owned.Remove(component);

      owned.Add(item);
    }

    return owned;
  }

  public static int Value(IEnumerable<string> items, Func<string, int> costOf) => items.Sum(costOf);

  public static (IReadOnlyList<string> Kept, IReadOnlyList<string> Removed) CapValue(
    IReadOnlyList<string> items,
    Func<string, int> costOf,
    int cap,
    int minItems = DefaultMinItems)
  {
    var kept = items.ToList();
    var removed = new List<string>();

    while (kept.Count > minItems && Value(kept, costOf) > cap)
    {
      var priciest = kept
        .Select((item, index) => (item, index))
        .OrderByDescending(entry => costOf(entry.item))
        .ThenByDescending(entry => entry.index)
        .First();

      kept.RemoveAt(priciest.index);
      removed.Add(priciest.item);
    }

    return (kept, removed);
  }

  public static bool TryParseCap(string? text, out int souls)
  {
    var trimmed = text?.Trim() ?? "";

    if (string.Equals(trimmed, DefaultCapWord, StringComparison.OrdinalIgnoreCase))
    {
      souls = DefaultCap;
      return true;
    }

    return int.TryParse(trimmed, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out souls)
      && souls is >= MinCap and <= MaxCap;
  }

  public static IReadOnlyList<(string Ability, int Bits)> AbilityBits(IEnumerable<AbilityStep> steps)
  {
    var order = new List<string>();
    var upgrades = new Dictionary<string, int>();

    foreach (var step in steps)
    {
      if (step.Kind != AbilityStep.Unlock && step.Kind != AbilityStep.Upgrade)
        continue;

      if (!upgrades.ContainsKey(step.Ability))
      {
        order.Add(step.Ability);
        upgrades[step.Ability] = 0;
      }

      if (step.Kind == AbilityStep.Upgrade && upgrades[step.Ability] < MaxUpgrades)
        upgrades[step.Ability]++;
    }

    return order.Select(ability => (ability, BitsFor(upgrades[ability]))).ToList();
  }

  public static AbilityPlan AbilityPrefix(IEnumerable<AbilityStep> steps, int unlocks, int points)
  {
    var order = new List<string>();
    var upgrades = new Dictionary<string, int>();
    var known = steps.Where(step => step.Kind == AbilityStep.Unlock || step.Kind == AbilityStep.Upgrade).ToList();
    var unlocksUsed = 0;
    var pointsUsed = 0;
    var taken = 0;

    foreach (var step in known)
    {
      var unlocked = upgrades.TryGetValue(step.Ability, out var tiers);
      var unlockCost = unlocked ? 0 : 1;
      var pointCost = step.Kind == AbilityStep.Upgrade && tiers < MaxUpgrades ? UpgradeCosts[tiers] : 0;

      if (unlocksUsed + unlockCost > unlocks || pointsUsed + pointCost > points)
        break;

      if (!unlocked)
      {
        order.Add(step.Ability);
        upgrades[step.Ability] = 0;
      }

      if (pointCost > 0)
        upgrades[step.Ability]++;

      unlocksUsed += unlockCost;
      pointsUsed += pointCost;
      taken++;
    }

    return new AbilityPlan(
      order.Select(ability => (ability, BitsFor(upgrades[ability]))).ToList(),
      taken,
      known.Count,
      unlocksUsed,
      pointsUsed);
  }

  public static int BitsFor(int upgrades)
  {
    if (upgrades >= MaxUpgrades)
      return FullyUpgradedBits;

    var bits = 1;

    for (var tier = 1; tier <= upgrades; tier++)
      bits |= 1 << tier;

    return bits;
  }
}
