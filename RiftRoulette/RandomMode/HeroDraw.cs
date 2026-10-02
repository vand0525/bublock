using DeadworksManaged.Api;

namespace RiftRoulette.RandomMode;

public static class HeroDraw
{
  public const int Attempts = 20;

  public static IReadOnlyDictionary<ulong, Heroes> Draw(
    IReadOnlyList<ulong> players,
    IReadOnlyList<Heroes> pool,
    IReadOnlyDictionary<ulong, Heroes> previous,
    Random rng,
    IReadOnlyDictionary<ulong, Heroes>? fixedHeroes = null,
    IReadOnlyCollection<Heroes>? mustInclude = null)
  {
    var fixedHere = (fixedHeroes ?? new Dictionary<ulong, Heroes>())
      .Where(pair => players.Contains(pair.Key))
      .ToDictionary(pair => pair.Key, pair => pair.Value);

    var rest = players.Where(player => !fixedHere.ContainsKey(player)).ToList();
    var free = pool.Where(hero => !fixedHere.ContainsValue(hero)).ToList();
    var restPool = free.Count > 0 ? free : pool;
    var priority = (mustInclude ?? [])
      .Distinct()
      .Where(hero => restPool.Contains(hero) && !fixedHere.ContainsValue(hero))
      .ToList();

    var drawn = DrawOnce(rest, restPool, previous, rng, priority);

    for (var attempt = 1; attempt < Attempts && Repeats(drawn, previous); attempt++)
      drawn = DrawOnce(rest, restPool, previous, rng, priority);

    foreach (var (player, hero) in fixedHere)
      drawn[player] = hero;

    return drawn;
  }

  private static bool Repeats(Dictionary<ulong, Heroes> drawn, IReadOnlyDictionary<ulong, Heroes> previous) =>
    drawn.Any(pair => previous.TryGetValue(pair.Key, out var last) && pair.Value == last);

  private static Dictionary<ulong, Heroes> DrawOnce(
    IReadOnlyList<ulong> players,
    IReadOnlyList<Heroes> pool,
    IReadOnlyDictionary<ulong, Heroes> previous,
    Random rng,
    IReadOnlyList<Heroes> priority)
  {
    var drawn = new Dictionary<ulong, Heroes>();

    if (pool.Count == 0)
      return drawn;

    var available = Shuffled(pool, rng);
    var waiting = Shuffled(players, rng);

    // Priority heroes go out first, each to a random player who did not have it last round when possible.
    foreach (var hero in Shuffled(priority, rng))
    {
      if (waiting.Count == 0)
        break;

      var index = waiting.FindIndex(player => !previous.TryGetValue(player, out var last) || last != hero);

      if (index < 0)
        index = 0;

      drawn[waiting[index]] = hero;
      waiting.RemoveAt(index);
      available.Remove(hero);
    }

    foreach (var player in waiting)
    {
      bool IsNew(Heroes hero) => !previous.TryGetValue(player, out var last) || hero != last;

      var index = available.FindIndex(IsNew);

      if (index < 0 && available.Count > 0)
        index = 0;

      if (index >= 0)
      {
        drawn[player] = available[index];
        available.RemoveAt(index);
        continue;
      }

      var fresh = pool.Where(IsNew).ToList();
      var from = fresh.Count > 0 ? fresh : pool;
      drawn[player] = from[rng.Next(from.Count)];
    }

    return drawn;
  }

  private static List<T> Shuffled<T>(IReadOnlyList<T> source, Random rng)
  {
    var list = source.ToList();

    for (var i = list.Count - 1; i > 0; i--)
    {
      var j = rng.Next(i + 1);
      (list[i], list[j]) = (list[j], list[i]);
    }

    return list;
  }
}
