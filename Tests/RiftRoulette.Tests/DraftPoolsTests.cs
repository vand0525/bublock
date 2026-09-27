using DeadworksManaged.Api;
using RiftRoulette.Draft;

namespace Bublock.Tests.RiftRoulette;

public class DraftPoolsTests
{
  [Theory]
  [InlineData(Heroes.Shiv, 3)]
  [InlineData(Heroes.Viper, 3)]
  [InlineData(Heroes.Fencer, 2)]
  [InlineData(Heroes.Magician, 2)]
  [InlineData(Heroes.Skyrunner, 0)]
  public void TeamOf_maps_heroes_to_team_numbers(Heroes hero, int expected)
  {
    Assert.Equal(expected, DraftPools.TeamOf(hero));
  }

  [Fact]
  public void Pools_have_six_heroes_and_do_not_overlap()
  {
    Assert.Equal(6, DraftPools.Sapphire.Count);
    Assert.Equal(6, DraftPools.Amber.Count);
    Assert.Empty(DraftPools.Sapphire.Intersect(DraftPools.Amber));
  }
}
