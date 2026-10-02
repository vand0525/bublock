using DeadworksManaged.Api;
using RiftRoulette.Mirror;

namespace Bublock.Tests.RiftRoulette;

public class MirrorPickTests
{
  private static readonly IReadOnlyList<Heroes> Pool = [Heroes.Haze, Heroes.Shiv, Heroes.Wraith, Heroes.Lash];

  private static int ThreeBuilds(Heroes hero) => 3;

  [Fact]
  public void Unpinned_returns_one_pool_hero_and_a_valid_build()
  {
    for (var seed = 0; seed < 50; seed++)
    {
      var choice = MirrorPick.Resolve(MirrorPins.None, Pool, ThreeBuilds, null, new Random(seed));

      Assert.NotNull(choice);
      Assert.Contains(choice.Hero, Pool);
      Assert.InRange(choice.BuildIndex, 0, 2);
    }
  }

  [Fact]
  public void Unpinned_never_repeats_last_rounds_hero()
  {
    for (var seed = 0; seed < 50; seed++)
      Assert.NotEqual(Heroes.Haze, MirrorPick.Resolve(MirrorPins.None, Pool, ThreeBuilds, Heroes.Haze, new Random(seed))!.Hero);
  }

  [Fact]
  public void Unpinned_skips_heroes_without_builds()
  {
    for (var seed = 0; seed < 50; seed++)
    {
      var choice = MirrorPick.Resolve(MirrorPins.None, Pool, hero => hero == Heroes.Lash ? 2 : 0, null, new Random(seed));
      Assert.Equal(Heroes.Lash, choice!.Hero);
    }

    Assert.Null(MirrorPick.Resolve(MirrorPins.None, Pool, _ => 0, null, new Random(1)));
  }

  [Fact]
  public void Hero_pin_keeps_the_hero_and_rolls_one_build()
  {
    var pins = MirrorPins.None.PinHero(Heroes.Kelvin, 3);
    var builds = new HashSet<int>();

    for (var seed = 0; seed < 50; seed++)
    {
      var choice = MirrorPick.Resolve(pins, Pool, ThreeBuilds, Heroes.Kelvin, new Random(seed))!;
      Assert.Equal(Heroes.Kelvin, choice.Hero);
      Assert.InRange(choice.BuildIndex, 0, 2);
      builds.Add(choice.BuildIndex);
    }

    Assert.True(builds.Count > 1);
  }

  [Fact]
  public void Hero_and_build_pins_always_give_that_slot()
  {
    Assert.True(MirrorPins.None.PinHero(Heroes.Kelvin, 3).TryPinSlot(2, 3, out var pins));

    for (var seed = 0; seed < 50; seed++)
      Assert.Equal(new MirrorChoice(Heroes.Kelvin, 1), MirrorPick.Resolve(pins, Pool, ThreeBuilds, null, new Random(seed)));
  }

  [Fact]
  public void Build_pin_needs_a_hero_and_a_slot_in_range()
  {
    Assert.False(MirrorPins.None.TryPinSlot(1, 3, out var unchanged));
    Assert.Equal(MirrorPins.None, unchanged);

    var hero = MirrorPins.None.PinHero(Heroes.Kelvin, 3);
    Assert.False(hero.TryPinSlot(0, 3, out _));
    Assert.False(hero.TryPinSlot(4, 3, out _));
    Assert.True(hero.TryPinSlot(3, 3, out var pinned));
    Assert.Equal(3, pinned.Slot);
  }

  [Fact]
  public void Slot_without_a_hero_is_ignored_and_the_build_rolls()
  {
    var orphan = new MirrorPins(null, 3);
    var builds = new HashSet<int>();

    for (var seed = 0; seed < 50; seed++)
      builds.Add(MirrorPick.Resolve(orphan, Pool, ThreeBuilds, null, new Random(seed))!.BuildIndex);

    Assert.True(builds.Count > 1);
  }

  [Fact]
  public void Changing_the_hero_keeps_the_slot_only_when_the_new_hero_has_it()
  {
    Assert.True(MirrorPins.None.PinHero(Heroes.Kelvin, 3).TryPinSlot(3, 3, out var pins));

    Assert.Equal(3, pins.PinHero(Heroes.Haze, 3).Slot);
    Assert.Null(pins.PinHero(Heroes.Haze, 2).Slot);
    Assert.Null(pins.ClearSlot().Slot);
    Assert.Equal(Heroes.Kelvin, pins.ClearSlot().Hero);
  }

  [Fact]
  public void Share_gives_every_fighter_the_same_hero_and_build()
  {
    var fighters = new ulong[] { 1, 2, 3, 4, 5, 6, 3 };

    for (var seed = 0; seed < 50; seed++)
    {
      var choice = MirrorPick.Resolve(MirrorPins.None, Pool, ThreeBuilds, null, new Random(seed))!;
      var shared = MirrorPick.Share(fighters, choice);

      Assert.Equal(6, shared.Count);
      Assert.All(shared.Values, assigned => Assert.Equal(choice, assigned));
      Assert.Single(shared.Values.Distinct());
    }
  }
}
