using DeadworksManaged.Api;
using RiftRoulette.RandomMode;

namespace Bublock.Tests.RiftRoulette;

public class PriorityHeroesTests
{
  [Fact]
  public void InPool_keeps_listed_heroes_that_have_builds_in_pool_order()
  {
    var list = new HashSet<Heroes> { Heroes.Kelvin, Heroes.Haze, Heroes.Bebop };
    var pool = new[] { Heroes.Haze, Heroes.Shiv, Heroes.Kelvin };

    Assert.Equal([Heroes.Haze, Heroes.Kelvin], PriorityHeroes.InPool(list, pool));
  }

  [Fact]
  public void InPool_with_an_empty_list_is_empty()
  {
    Assert.Empty(PriorityHeroes.InPool(new HashSet<Heroes>(), [Heroes.Haze, Heroes.Shiv]));
  }

  [Fact]
  public void RefusedLine_names_the_hero_and_the_action()
  {
    Assert.Equal(
      "Haze is a priority hero: someone gets it every round, so it can't be reserved.",
      PriorityHeroes.RefusedLine("Haze", "reserved"));
    Assert.Equal(
      "Haze is a priority hero: someone gets it every round, so it can't be banned.",
      PriorityHeroes.RefusedLine("Haze", "banned"));
  }
}
