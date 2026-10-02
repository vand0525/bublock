using DeadworksManaged.Api;

namespace RiftRoulette.Round;

public static class RoundHeroes
{
  private static readonly Dictionary<ulong, Heroes> Entries = [];

  public static int Count => Entries.Count;

  public static IEnumerable<(ulong SteamId, Heroes Hero)> All =>
    Entries.Select(entry => (entry.Key, entry.Value));

  public static bool Has(ulong steamId) => Entries.ContainsKey(steamId);

  public static bool TryGet(ulong steamId, out Heroes hero) =>
    Entries.TryGetValue(steamId, out hero);

  public static void Set(ulong steamId, Heroes hero) => Entries[steamId] = hero;

  public static bool Remove(ulong steamId, out Heroes hero) => Entries.Remove(steamId, out hero);

  public static void Clear() => Entries.Clear();
}
