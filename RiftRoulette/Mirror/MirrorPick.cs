using DeadworksManaged.Api;

namespace RiftRoulette.Mirror;

public sealed record MirrorChoice(Heroes Hero, int BuildIndex);

public sealed record MirrorPins(Heroes? Hero, int? Slot)
{
  public static MirrorPins None { get; } = new(null, null);

  // Slots are 1-based (what admins type); a slot the new hero lacks is dropped.
  public MirrorPins PinHero(Heroes hero, int buildCount) =>
    new(hero, Slot is { } slot && slot <= buildCount ? slot : null);

  public bool TryPinSlot(int slot, int buildCount, out MirrorPins pinned)
  {
    pinned = this;

    if (Hero == null || slot < 1 || slot > buildCount)
      return false;

    pinned = this with { Slot = slot };
    return true;
  }

  public MirrorPins ClearSlot() => this with { Slot = null };
}

public static class MirrorPick
{
  public static MirrorChoice? Resolve(
    MirrorPins pins,
    IReadOnlyList<Heroes> pool,
    Func<Heroes, int> buildCount,
    Heroes? last,
    Random rng)
  {
    var hero = pins.Hero is { } pinned && buildCount(pinned) > 0 ? pinned : RollHero(pool, buildCount, last, rng);

    if (hero is not { } chosen)
      return null;

    var count = buildCount(chosen);
    var slot = pins.Hero == chosen ? pins.Slot : null;
    var index = slot is { } fixedSlot && fixedSlot <= count ? fixedSlot - 1 : rng.Next(count);

    return new MirrorChoice(chosen, index);
  }

  public static IReadOnlyDictionary<ulong, MirrorChoice> Share(IEnumerable<ulong> fighters, MirrorChoice choice) =>
    fighters.Distinct().ToDictionary(steamId => steamId, _ => choice);

  private static Heroes? RollHero(IReadOnlyList<Heroes> pool, Func<Heroes, int> buildCount, Heroes? last, Random rng)
  {
    var withBuilds = pool.Where(hero => buildCount(hero) > 0).ToList();

    if (withBuilds.Count == 0)
      return null;

    var fresh = withBuilds.Where(hero => hero != last).ToList();
    var from = fresh.Count > 0 ? fresh : withBuilds;
    return from[rng.Next(from.Count)];
  }
}
