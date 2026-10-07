using DeadworksManaged.Api;
using RiftRoulette.RandomMode;

namespace Bublock.Tests.RiftRoulette;

public class PriorityHeroesTests
{
  [Fact]
  public void List_holds_rat_king_and_baba()
  {
    Assert.True(PriorityHeroes.Contains(Heroes.RatKing));
    Assert.True(PriorityHeroes.Contains(Heroes.Baba));
    Assert.Equal(2, PriorityHeroes.List.Count);
  }

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
  public void RerollPool_never_offers_a_failed_hero_and_prefers_unheld_ones()
  {
    var pool = new[] { Heroes.RatKing, Heroes.Haze, Heroes.Shiv, Heroes.Kelvin };
    var failed = new HashSet<Heroes> { Heroes.RatKing };

    Assert.Equal([Heroes.Shiv, Heroes.Kelvin], PriorityHeroes.RerollPool(pool, failed, new HashSet<Heroes> { Heroes.Haze }));
  }

  [Fact]
  public void RerollPool_allows_a_held_hero_when_every_other_is_taken()
  {
    var pool = new[] { Heroes.RatKing, Heroes.Haze };
    var failed = new HashSet<Heroes> { Heroes.RatKing };

    Assert.Equal([Heroes.Haze], PriorityHeroes.RerollPool(pool, failed, new HashSet<Heroes> { Heroes.Haze }));
  }

  [Fact]
  public void RerollPool_is_empty_when_only_failed_heroes_are_left()
  {
    Assert.Empty(PriorityHeroes.RerollPool([Heroes.RatKing], new HashSet<Heroes> { Heroes.RatKing }, new HashSet<Heroes>()));
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
