using DeadworksManaged.Api;

namespace RiftRoulette.RandomMode;

public static class PriorityHeroes
{
  // Change by deploying. A hero only takes part once it has stored builds (HeroBuildCatalog.Heroes).
  public static readonly IReadOnlySet<Heroes> List = new HashSet<Heroes> { Heroes.RatKing };

  public static bool Contains(Heroes hero) => List.Contains(hero);

  public static IReadOnlyList<Heroes> InPool(IReadOnlyList<Heroes> pool) => InPool(List, pool);

  public static IReadOnlyList<Heroes> InPool(IReadOnlySet<Heroes> list, IReadOnlyList<Heroes> pool) =>
    pool.Where(list.Contains).ToList();

  public static string RefusedLine(string name, string action) =>
    $"{name} is a priority hero: someone gets it every round, so it can't be {action}.";
}
