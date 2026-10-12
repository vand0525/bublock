using DeadworksManaged.Api;
using RiftRoulette.RandomMode;

namespace Bublock.Tests.RiftRoulette;

public class HeroDrawTests
{
  private static readonly IReadOnlyList<Heroes> Pool =
    [Heroes.Haze, Heroes.Shiv, Heroes.Wraith, Heroes.Lash, Heroes.Kelvin, Heroes.Dynamo];

  private static readonly IReadOnlyDictionary<ulong, Heroes> None = new Dictionary<ulong, Heroes>();

  [Fact]
  public void Draw_gives_every_player_a_unique_hero_from_the_pool()
  {
    var players = new ulong[] { 1, 2, 3, 4 };

    for (var seed = 0; seed < 50; seed++)
    {
      var drawn = HeroDraw.Draw(players, Pool, None, new Random(seed));

      Assert.Equal(players.Order(), drawn.Keys.Order());
      Assert.All(drawn.Values, hero => Assert.Contains(hero, Pool));
      Assert.Equal(drawn.Count, drawn.Values.Distinct().Count());
    }
  }

  [Fact]
  public void Draw_never_repeats_a_players_previous_hero()
  {
    var players = new ulong[] { 1, 2, 3, 4, 5, 6 };
    var previous = players.Zip(Pool).ToDictionary(pair => pair.First, pair => pair.Second);

    for (var seed = 0; seed < 50; seed++)
    {
      var drawn = HeroDraw.Draw(players, Pool, previous, new Random(seed));

      Assert.All(players, player => Assert.NotEqual(previous[player], drawn[player]));
      Assert.Equal(players.Length, drawn.Values.Distinct().Count());
    }
  }

  [Fact]
  public void Draw_with_a_one_hero_pool_repeats_rather_than_skipping()
  {
    var previous = new Dictionary<ulong, Heroes> { [1] = Heroes.Haze };

    Assert.Equal(Heroes.Haze, HeroDraw.Draw([1], [Heroes.Haze], previous, new Random(1))[1]);
  }

  [Fact]
  public void Draw_with_more_players_than_heroes_still_assigns_everyone()
  {
    var drawn = HeroDraw.Draw([1, 2, 3], [Heroes.Haze, Heroes.Shiv], None, new Random(3));

    Assert.Equal(3, drawn.Count);
  }

  [Fact]
  public void Draw_gives_fixed_heroes_and_never_draws_them_for_anyone_else()
  {
    var players = new ulong[] { 1, 2, 3, 4, 5 };
    var fixedHeroes = new Dictionary<ulong, Heroes> { [1] = Heroes.Haze, [9] = Heroes.Shiv };
    var previous = new Dictionary<ulong, Heroes> { [1] = Heroes.Haze };

    for (var seed = 0; seed < 50; seed++)
    {
      var drawn = HeroDraw.Draw(players, Pool, previous, new Random(seed), fixedHeroes);

      Assert.Equal(players.Order(), drawn.Keys.Order());
      Assert.Equal(Heroes.Haze, drawn[1]);
      Assert.DoesNotContain(players.Skip(1), player => drawn[player] == Heroes.Haze);
      Assert.Equal(drawn.Count, drawn.Values.Distinct().Count());
    }
  }

  [Fact]
  public void Draw_with_no_fixed_heroes_matches_the_plain_draw()
  {
    var players = new ulong[] { 1, 2, 3 };

    Assert.Equal(
      HeroDraw.Draw(players, Pool, None, new Random(7)),
      HeroDraw.Draw(players, Pool, None, new Random(7), new Dictionary<ulong, Heroes>()));
  }

  [Fact]
  public void Draw_with_an_empty_pool_assigns_nobody()
  {
    Assert.Empty(HeroDraw.Draw([1, 2], [], None, new Random(0)));
  }
}
