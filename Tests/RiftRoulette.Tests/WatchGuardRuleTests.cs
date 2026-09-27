using RiftRoulette.Locations;
using RiftRoulette.Round;

namespace Bublock.Tests.RiftRoulette;

public class WatchGuardRuleTests
{
  private const float SpotZ = 1536.0625f;

  [Fact]
  public void Line_leaves_the_margin_below_the_spot()
  {
    Assert.Equal(SpotZ - WatchGuardRule.Margin, WatchGuardRule.Line(SpotZ));
  }

  [Theory]
  [InlineData(1536.0625f, false)]
  [InlineData(1700f, false)]
  [InlineData(1300f, false)]
  [InlineData(1200f, true)]
  [InlineData(250f, true)]
  public void IsBelow_only_after_a_real_drop(float z, bool expected)
  {
    Assert.Equal(expected, WatchGuardRule.IsBelow(z, SpotZ));
  }

  [Fact]
  public void Line_is_well_above_the_rift_starts()
  {
    var line = WatchGuardRule.Line(RiftRouletteLocations.WatchGreen.Position.Z);

    Assert.True(line > RiftRouletteLocations.GreenSapphire.Position.Z + 500f);
    Assert.True(line > RiftRouletteLocations.YellowAmber.Position.Z + 500f);
  }
}
