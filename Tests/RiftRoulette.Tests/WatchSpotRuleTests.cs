using RiftRoulette.Rift;
using RiftRoulette.Round;

namespace Bublock.Tests.RiftRoulette;

public class WatchSpotRuleTests
{
  [Fact]
  public void Running_rift_uses_the_side_being_fought_on()
  {
    Assert.Equal(RiftSide.Yellow, WatchSpotRule.SideFor(true, RiftSide.Yellow, RiftSide.Green));
  }

  [Fact]
  public void Idle_uses_the_next_side()
  {
    Assert.Equal(RiftSide.Green, WatchSpotRule.SideFor(false, RiftSide.Yellow, RiftSide.Green));
    Assert.Equal(RiftSide.Yellow, WatchSpotRule.SideFor(false, null, RiftSide.Yellow));
  }

  [Fact]
  public void Running_without_a_current_side_falls_back_to_the_next_side()
  {
    Assert.Equal(RiftSide.Green, WatchSpotRule.SideFor(true, null, RiftSide.Green));
  }
}
