using DeadworksManaged.Api;

namespace Bublock.Modules.RandomLoadout;

public static class HeroRoll
{
  // A random hero that is not the player's current one, preferring heroes nobody else has.
  public static Heroes? Pick(IReadOnlyList<Heroes> pool, Heroes? current, IReadOnlyCollection<Heroes> taken, Random rng)
  {
    var others = pool.Where(hero => current is not { } now || hero != now).ToList();
    var free = others.Where(hero => !taken.Contains(hero)).ToList();
    var choices = free.Count > 0 ? free : others;

    return choices.Count == 0 ? null : choices[rng.Next(choices.Count)];
  }
}
