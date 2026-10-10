using DeadworksManaged.Api;

namespace RiftRoulette.RandomMode;

public static class PriorityHeroes
{
  // Change by deploying. A hero only takes part once it has stored builds (HeroBuildCatalog.Heroes).
  public static readonly IReadOnlySet<Heroes> List = new HashSet<Heroes> { Heroes.RatKing, Heroes.Baba, Heroes.Chessmaster };

  // Below this many fighters a round draws priority heroes like any other hero.
  public const int MinFighters = 8;

  public static bool Contains(Heroes hero) => List.Contains(hero);

  public static bool Guaranteed(int fighters) => fighters >= MinFighters;

  public static IReadOnlyList<Heroes> InPool(IReadOnlyList<Heroes> pool) => InPool(List, pool);

  public static IReadOnlyList<Heroes> InPool(IReadOnlySet<Heroes> list, IReadOnlyList<Heroes> pool) =>
    pool.Where(list.Contains).ToList();

  // Heroes for a player whose priority hero would not spawn: never a failed hero, an unheld one when possible.
  public static IReadOnlyList<Heroes> RerollPool(
    IReadOnlyList<Heroes> pool,
    IReadOnlySet<Heroes> failed,
    IReadOnlySet<Heroes> taken)
  {
    var working = pool.Where(hero => !failed.Contains(hero)).ToList();
    var free = working.Where(hero => !taken.Contains(hero)).ToList();
    return free.Count > 0 ? free : working;
  }

  public static string RefusedLine(string name, string action) =>
    $"{name} is a priority hero: with {MinFighters} or more fighters someone gets it every round, so it can't be {action}.";
}
