using Bublock.Modules.RandomLoadout;
using DeadworksManaged.Api;

namespace Bublock.Tests.Modules;

public class HeroRollTests
{
  private static readonly Heroes[] Pool = [Heroes.Inferno, Heroes.Gigawatt, Heroes.Hornet];

  [Fact]
  public void Never_returns_the_current_hero()
  {
    for (var seed = 0; seed < 50; seed++)
      Assert.NotEqual(Heroes.Inferno, HeroRoll.Pick(Pool, Heroes.Inferno, [], new Random(seed)));
  }

  [Fact]
  public void Prefers_heroes_nobody_else_holds()
  {
    for (var seed = 0; seed < 50; seed++)
      Assert.Equal(Heroes.Hornet, HeroRoll.Pick(Pool, Heroes.Inferno, [Heroes.Gigawatt], new Random(seed)));
  }

  [Fact]
  public void Falls_back_to_a_taken_hero_and_gives_up_on_a_pool_of_one()
  {
    Assert.Equal(Heroes.Gigawatt, HeroRoll.Pick([Heroes.Inferno, Heroes.Gigawatt], Heroes.Inferno, [Heroes.Gigawatt], new Random(1)));
    Assert.Null(HeroRoll.Pick([Heroes.Inferno], Heroes.Inferno, [], new Random(1)));
    Assert.NotNull(HeroRoll.Pick(Pool, null, [], new Random(1)));
  }
}
