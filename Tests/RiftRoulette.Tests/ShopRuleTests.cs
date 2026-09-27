using RiftRoulette.GameLoop;

namespace Bublock.Tests.RiftRoulette;

public class ShopRuleTests
{
  [Theory]
  [InlineData(true, false, true)]
  [InlineData(true, true, false)]
  [InlineData(false, false, false)]
  [InlineData(false, true, false)]
  public void Buying_is_open_only_in_1v1_setup(bool isDuel, bool matchRunning, bool expected)
  {
    Assert.Equal(expected, ShopRule.BuyAnywhere(isDuel, matchRunning));
  }
}
