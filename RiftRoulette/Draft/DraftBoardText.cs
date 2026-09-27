using DeadworksManaged.Api;

namespace RiftRoulette.Draft;

public static class DraftBoardText
{
  public static string TeamBoard(string teamName, IEnumerable<Heroes> heroes, Func<Heroes, bool> isSelected)
  {
    var heroList = string.Join(
      "\n",
      heroes.Select(hero => isSelected(hero) ? $"{hero} (SELECTED)" : hero.ToString()));

    return $"{teamName}\n\n{heroList}\n\n/pick <hero>\n/unpick";
  }

  public static string PoolLine(string teamName, IEnumerable<Heroes> heroes, Func<Heroes, bool> isSelected) =>
    $"{teamName}: " + string.Join(", ", heroes.Select(hero => isSelected(hero) ? $"{hero} (taken)" : hero.ToString()));
}
