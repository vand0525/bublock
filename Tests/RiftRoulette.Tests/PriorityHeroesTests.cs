using DeadworksManaged.Api;
using RiftRoulette.RandomMode;

namespace Bublock.Tests.RiftRoulette;

public class PriorityHeroesTests
{
  [Fact]
  public void List_holds_rat_king_baba_and_solomon()
  {
    Assert.True(PriorityHeroes.Contains(Heroes.RatKing));
    Assert.True(PriorityHeroes.Contains(Heroes.Baba));
    Assert.True(PriorityHeroes.Contains(Heroes.Chessmaster));
    Assert.Equal(3, PriorityHeroes.List.Count);
  }

  [Theory]
  [InlineData(2, false)]
  [InlineData(7, false)]
  [InlineData(8, true)]
  [InlineData(12, true)]
  public void Guaranteed_only_with_eight_or_more_fighters(int fighters, bool expected)
  {
    Assert.Equal(expected, PriorityHeroes.Guaranteed(fighters));
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
      "Haze is a priority hero: with 8 or more fighters someone gets it every round, so it can't be reserved.",
      PriorityHeroes.RefusedLine("Haze", "reserved"));
    Assert.Equal(
      "Haze is a priority hero: with 8 or more fighters someone gets it every round, so it can't be banned.",
      PriorityHeroes.RefusedLine("Haze", "banned"));
  }
}
