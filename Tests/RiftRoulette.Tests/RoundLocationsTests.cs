using RiftRoulette.Locations;
using RiftRoulette.Rift;
using RiftRoulette.Round;

namespace Bublock.Tests.RiftRoulette;

public class RoundLocationsTests
{
  [Theory]
  [InlineData(RiftSide.Green, "green_sapphire", "green_amber")]
  [InlineData(RiftSide.Yellow, "yellow_sapphire", "yellow_amber")]
  [InlineData(RiftSide.Center, "center_sapphire", "center_amber")]
  public void StartsFor_maps_each_side_to_its_team_starts(RiftSide side, string sapphire, string amber)
  {
    var starts = RoundLocations.StartsFor(side);

    Assert.Equal(sapphire, starts.Sapphire.Name);
    Assert.Equal(amber, starts.Amber.Name);
  }

  [Theory]
  [InlineData(RiftSide.Green)]
  [InlineData(RiftSide.Yellow)]
  [InlineData(RiftSide.Center)]
  public void StartsFor_puts_each_team_on_its_own_half(RiftSide side)
  {
    var starts = RoundLocations.StartsFor(side);

    Assert.True(starts.Sapphire.Position.Y > 0f, "Sapphire (team 3) base is at +y");
    Assert.True(starts.Amber.Position.Y < 0f, "Amber (team 2) base is at -y");
  }

  [Theory]
  [InlineData(RiftSide.Green, "watch_green")]
  [InlineData(RiftSide.Yellow, "watch_yellow")]
  [InlineData(RiftSide.Center, "watch_center")]
  public void WatchFor_maps_each_side_to_the_spot_above_its_rift(RiftSide side, string name)
  {
    var spot = RoundLocations.WatchFor(side);
    var starts = RoundLocations.StartsFor(side);

    Assert.Equal(name, spot.Name);
    Assert.True(spot.Position.Z > starts.Sapphire.Position.Z + 500f);
    Assert.Equal(spot.Position.Z, RiftRouletteLocations.Draft.Position.Z);
  }
}
