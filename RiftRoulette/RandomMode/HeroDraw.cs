using DeadworksManaged.Api;

namespace RiftRoulette.RandomMode;

public static class HeroDraw
{
  public const int Attempts = 20;

  public static IReadOnlyDictionary<ulong, Heroes> Draw(
    IReadOnlyList<ulong> players,
    IReadOnlyList<Heroes> pool,
    IReadOnlyDictionary<ulong, Heroes> previous,
    Random rng)
  {
    var drawn = DrawOnce(players, pool, previous, rng);

    for (var attempt = 1; attempt < Attempts && Repeats(drawn, previous); attempt++)
      drawn = DrawOnce(players, pool, previous, rng);

    return drawn;
  }

  private static bool Repeats(Dictionary<ulong, Heroes> drawn, IReadOnlyDictionary<ulong, Heroes> previous) =>
    drawn.Any(pair => previous.TryGetValue(pair.Key, out var last) && pair.Value == last);

  private static Dictionary<ulong, Heroes> DrawOnce(
    IReadOnlyList<ulong> players,
    IReadOnlyList<Heroes> pool,
    IReadOnlyDictionary<ulong, Heroes> previous,
    Random rng)
  {
    var drawn = new Dictionary<ulong, Heroes>();

    if (pool.Count == 0)
      return drawn;

    var available = Shuffled(pool, rng);

    foreach (var player in Shuffled(players, rng))
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
