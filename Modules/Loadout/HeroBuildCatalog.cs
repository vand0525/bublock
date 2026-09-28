using Bublock.Shared;
using DeadworksManaged.Api;

namespace Bublock.Modules.Loadout;

public sealed class HeroBuildCatalog
{
  public const string ResourceName = "Bublock.Modules.Loadout.hero-builds.json";

  private static readonly Lazy<HeroBuildCatalog> Embedded = new(LoadEmbedded);

  private readonly Dictionary<Heroes, HeroBuildSet> _byHero = [];

  private readonly Dictionary<string, List<string>> _upgrades = [];

  public HeroBuildCatalog(HeroBuildData data)
  {
    Data = data;

    var skipped = new List<int>();

    foreach (var set in data.Heroes ?? [])
    {
      if (!Enum.IsDefined(typeof(Heroes), set.Id) || (set.Builds?.Count ?? 0) == 0)
      {
        skipped.Add(set.Id);
        continue;
      }

      _byHero[(Heroes)set.Id] = set;
    }

    foreach (var (upgrade, components) in data.Components ?? new Dictionary<string, IReadOnlyList<string>>())
    foreach (var component in components.Distinct())
    {
      if (!_upgrades.TryGetValue(component, out var upgrades))
        _upgrades[component] = upgrades = [];

      upgrades.Add(upgrade);
    }

    Heroes = _byHero.Keys.OrderBy(hero => (int)hero).ToList();
    SkippedHeroIds = skipped;
    BaselineValue = Median(_byHero.Values.SelectMany(set => set.Builds ?? []).Select(build => PlannedValue(build)));
  }

  public int BaselineValue { get; }

  public int CostOf(string item) =>
    Data.ItemCosts != null && Data.ItemCosts.TryGetValue(item, out var cost) ? cost : 0;

  public ShopPlan Plan(HeroBuild build, int budget = LoadoutPlanner.DefaultCap, int slots = LoadoutPlanner.DefaultSlots) =>
    LoadoutPlanner.Plan(
      LoadoutPlanner.ItemOrder(build),
      ComponentsOf,
      CostOf,
      budget,
      slots,
      build.SellPriorityOf,
      fillers: LoadoutPlanner.OptionalItems(build),
      upgradesOf: UpgradesOf);

  public int PlannedValue(HeroBuild build, int budget = LoadoutPlanner.DefaultCap, int slots = LoadoutPlanner.DefaultSlots) =>
    Plan(build, budget, slots).Value;

  public static HeroBuildCatalog Default => Embedded.Value;

  public HeroBuildData Data { get; }

  public IReadOnlyList<Heroes> Heroes { get; }

  public IReadOnlyList<int> SkippedHeroIds { get; }

  public IReadOnlyList<HeroBuild> BuildsFor(Heroes hero) =>
    _byHero.TryGetValue(hero, out var set) ? set.Builds ?? [] : [];

  public string DisplayName(Heroes hero) =>
    _byHero.TryGetValue(hero, out var set) && set.Name.Length > 0 ? set.Name : hero.ToString();

  public IReadOnlyList<string> ComponentsOf(string item) =>
    Data.Components != null && Data.Components.TryGetValue(item, out var components) ? components : [];

  public IReadOnlyList<string> UpgradesOf(string item) =>
    _upgrades.TryGetValue(item, out var upgrades) ? upgrades : [];

  public bool TryParseHero(string text, out Heroes hero)
  {
    if (Enum.TryParse(text, true, out hero) && Enum.IsDefined(hero))
      return true;

    foreach (var (candidate, set) in _byHero)
    {
      if (!string.Equals(set.Name, text, StringComparison.OrdinalIgnoreCase))
        continue;

      hero = candidate;
      return true;
    }

    return false;
  }

  private static int Median(IEnumerable<int> values)
  {
    var sorted = values.OrderBy(value => value).ToList();

    if (sorted.Count == 0)
      return 0;

    var middle = sorted.Count / 2;
    return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
  }

  private static HeroBuildCatalog LoadEmbedded()
  {
    using var stream = typeof(HeroBuildCatalog).Assembly.GetManifestResourceStream(ResourceName)
      ?? throw new InvalidOperationException($"Embedded resource {ResourceName} not found.");

    var catalog = new HeroBuildCatalog(HeroBuildData.Parse(stream));

    if (catalog.SkippedHeroIds.Count > 0)
    {
      BublockLog.For("Loadout").Warn(
        "Build data heroes skipped, not in the Heroes enum or without builds Ids={Ids} Pool={Pool}",
        string.Join(",", catalog.SkippedHeroIds),
        catalog.Heroes.Count);
    }

    return catalog;
  }
}
