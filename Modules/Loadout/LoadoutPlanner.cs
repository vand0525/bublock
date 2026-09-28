namespace Bublock.Modules.Loadout;

public sealed record AbilityPlan(
  IReadOnlyList<(string Ability, int Bits)> Bits,
  int StepsTaken,
  int StepsTotal,
  int UnlocksUsed,
  int PointsUsed);

public sealed record ShopPlan(
  IReadOnlyList<string> Items,
  int Value,
  IReadOnlyList<string> Bought,
  IReadOnlyList<string> Sold,
  IReadOnlyList<string> Skipped,
  IReadOnlyList<string> Filled,
  IReadOnlyList<string> Upgraded);

public static class LoadoutPlanner
{
  public const int DefaultSlots = 12;

  public const int MaxUpgrades = 3;

  public static readonly IReadOnlyList<int> UpgradeCosts = [1, 2, 5];

  public const int FullyUpgradedBits = 0b11111;

  public const int DefaultCap = 20000;
  public const int MinCap = 1000;
  public const int MaxCap = 200000;

  public const string DefaultCapWord = "default";

  public static readonly IReadOnlySet<string> Banned = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
  {
    "upgrade_non_player_bonus",
    "upgrade_non_player_bonus_sacrifice",
    "upgrade_goose_egg",
    "upgrade_trophy_collector",
    "upgrade_health_stimpak"
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

  public static IReadOnlyList<string> OptionalItems(IReadOnlyList<BuildCategory>? categories) =>
    (categories ?? [])
      .Where(category => category.Optional)
      .SelectMany(category => category.Items ?? [])
      .Where(item => !Banned.Contains(item))
      .Distinct()
      .ToList();

  public static IReadOnlyList<string> OptionalItems(HeroBuild build) => OptionalItems(build.Categories);

  public static ShopPlan Plan(
    IEnumerable<string> order,
    Func<string, IReadOnlyList<string>> componentsOf,
    Func<string, int> costOf,
    int budget,
    int slots = DefaultSlots,
    Func<string, int>? sellPriorityOf = null,
    Func<string, bool>? exists = null,
    IEnumerable<string>? fillers = null,
    Func<string, IReadOnlyList<string>>? upgradesOf = null)
  {
    var owned = new List<string>();
    var bought = new List<string>();
    var sold = new List<string>();
    var skipped = new List<string>();
    var filled = new List<string>();
    var upgraded = new List<string>();
    var value = 0;

    bool Usable(string item) =>
      !owned.Contains(item) && !Banned.Contains(item) && (exists == null || exists(item));

    bool TryBuy(string item, bool allowSale)
    {
      var components = componentsOf(item).Where(owned.Contains).Distinct().ToList();
      var cost = costOf(item);
      string? sale = null;

      if (owned.Count - components.Count >= slots)
      {
        if (!allowSale)
          return false;

        sale = SellCandidate(owned, components, cost, costOf, sellPriorityOf);

        if (sale == null)
          return false;
      }

      var after = value - components.Sum(costOf) + cost - (sale == null ? 0 : costOf(sale));

      if (after > budget)
        return false;

      if (sale != null)
      {
        owned.Remove(sale);
        sold.Add(sale);
      }

      foreach (var component in components)
        owned.Remove(component);

      owned.Add(item);
      bought.Add(item);
      value = after;
      return true;
    }

    foreach (var item in order)
    {
      if (Usable(item) && !TryBuy(item, allowSale: true))
        skipped.Add(item);
    }

    foreach (var item in (fillers ?? []).Distinct().OrderByDescending(costOf).ToList())
    {
      if (owned.Count >= slots)
        break;

      if (Usable(item) && TryBuy(item, allowSale: false))
        filled.Add(item);
    }

    if (upgradesOf == null)
      return new ShopPlan(owned, value, bought, sold, skipped, filled, upgraded);

    bool changed;

    do
    {
      changed = false;

      foreach (var item in owned.ToList())
      {
        if (!owned.Contains(item))
          continue;

        var options = upgradesOf(item)
          .Where(upgrade => costOf(upgrade) > costOf(item) && Usable(upgrade))
          .OrderByDescending(costOf);

        foreach (var upgrade in options)
        {
          if (!TryBuy(upgrade, allowSale: false))
            continue;

          upgraded.Add(upgrade);
          changed = true;
          break;
        }
      }
    }
    while (changed);

    return new ShopPlan(owned, value, bought, sold, skipped, filled, upgraded);
  }

  public static string? SellCandidate(
    IReadOnlyList<string> owned,
    IReadOnlyCollection<string> keep,
    int replacementCost,
    Func<string, int> costOf,
    Func<string, int>? sellPriorityOf = null) =>
    owned
      .Select((item, index) => (item, index, priority: Math.Max(0, sellPriorityOf?.Invoke(item) ?? 0)))
      .Where(entry => !keep.Contains(entry.item) && costOf(entry.item) < replacementCost)
      .OrderByDescending(entry => entry.priority)
      .ThenBy(entry => costOf(entry.item))
      .ThenBy(entry => entry.index)
      .Select(entry => entry.item)
      .FirstOrDefault();

  public static int Value(IEnumerable<string> items, Func<string, int> costOf) => items.Sum(costOf);

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
