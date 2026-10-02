using DeadworksManaged.Api;
using RiftRoulette.Round;

namespace Bublock.Tests.RiftRoulette;

public class RoundHeroesTests
{
  private const ulong Theo = 76561198000000001;
  private const ulong Other = 76561198000000002;

  public RoundHeroesTests()
  {
    RoundHeroes.Clear();
  }

  [Fact]
  public void Set_records_the_round_hero()
  {
    RoundHeroes.Set(Theo, Heroes.Shiv);

    Assert.True(RoundHeroes.Has(Theo));
    Assert.True(RoundHeroes.TryGet(Theo, out var hero));
    Assert.Equal(Heroes.Shiv, hero);
    Assert.Equal([(Theo, Heroes.Shiv)], RoundHeroes.All);
  }

  [Fact]
  public void Set_again_replaces_the_hero()
  {
    RoundHeroes.Set(Theo, Heroes.Shiv);
    RoundHeroes.Set(Theo, Heroes.Yamato);

    Assert.True(RoundHeroes.TryGet(Theo, out var hero));
    Assert.Equal(Heroes.Yamato, hero);
    Assert.Equal(1, RoundHeroes.Count);
  }

  [Fact]
  public void Two_players_may_hold_the_same_hero()
  {
    RoundHeroes.Set(Theo, Heroes.Shiv);
    RoundHeroes.Set(Other, Heroes.Shiv);

    Assert.Equal(2, RoundHeroes.Count);
  }

  [Fact]
  public void Remove_drops_only_that_player()
  {
    RoundHeroes.Set(Theo, Heroes.Shiv);
    RoundHeroes.Set(Other, Heroes.Fencer);

    Assert.True(RoundHeroes.Remove(Theo, out var hero));
    Assert.Equal(Heroes.Shiv, hero);
    Assert.False(RoundHeroes.Has(Theo));
    Assert.True(RoundHeroes.Has(Other));
  }

  [Fact]
  public void Remove_without_an_entry_returns_false()
  {
    Assert.False(RoundHeroes.Remove(Theo, out _));
    Assert.False(RoundHeroes.TryGet(Theo, out _));
  }

  [Fact]
  public void Clear_empties_everything()
  {
    RoundHeroes.Set(Theo, Heroes.Shiv);
    RoundHeroes.Set(Other, Heroes.Fencer);

    RoundHeroes.Clear();

    Assert.Equal(0, RoundHeroes.Count);
    Assert.False(RoundHeroes.Has(Theo));
  }
}
