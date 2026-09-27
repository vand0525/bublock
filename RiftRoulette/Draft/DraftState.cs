using DeadworksManaged.Api;

namespace RiftRoulette.Draft;

public static class DraftState
{
  private static readonly HashSet<Heroes> Selected = [];

  private static readonly Dictionary<ulong, Heroes> Picks = [];

  public static IEnumerable<Heroes> SelectedHeroes => Selected;

  public static IEnumerable<(ulong SteamId, Heroes Hero)> AllPicks =>
    Picks.Select(pick => (pick.Key, pick.Value));

  public static bool IsSelected(Heroes hero) => Selected.Contains(hero);

  public static bool HasPick(ulong steamId) => Picks.ContainsKey(steamId);

  public static bool TryGetPick(ulong steamId, out Heroes hero) =>
    Picks.TryGetValue(steamId, out hero);

  public static void Add(ulong steamId, Heroes hero)
  {
    Selected.Add(hero);
    Picks.Add(steamId, hero);
  }

  public static bool Release(ulong steamId, out Heroes hero)
  {
    if (!Picks.Remove(steamId, out hero))
      return false;

    Selected.Remove(hero);
    return true;
  }

  public static void Clear()
  {
    Selected.Clear();
    Picks.Clear();
  }
}
