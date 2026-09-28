using Bublock.Modules.Economy;
using DeadworksManaged.Api;

namespace Bublock.Tests.Modules;

public class EconomyTests
{
  [Theory]
  [InlineData(ECurrencySource.EPlayerKill, true)]
  [InlineData(ECurrencySource.ELevelUp, true)]
  [InlineData(ECurrencySource.ECheats, false)]
  [InlineData(ECurrencySource.EStartingAmount, false)]
  [InlineData(ECurrencySource.EItemSale, false)]
  public void Earned_gold_is_blocked_and_loadout_gold_passes(ECurrencySource source, bool blocked)
  {
    Assert.Equal(blocked, SoulRule.ShouldBlock(ECurrencyType.EGold, source, 500, active: true));
  }

  [Fact]
  public void Ability_points_pass_only_from_cheats_when_ranks_come_from_the_build()
  {
    Assert.True(SoulRule.ShouldBlock(ECurrencyType.EAbilityPoints, ECurrencySource.ELevelUp, 1, active: true));
    Assert.False(SoulRule.ShouldBlock(ECurrencyType.EAbilityPoints, ECurrencySource.ECheats, 1, active: true));
    Assert.False(SoulRule.ShouldBlock(ECurrencyType.EAbilityPoints, ECurrencySource.ELevelUp, 1, active: true, ranksFromBuild: false));
  }

  [Fact]
  public void Nothing_is_blocked_when_inactive_or_spending()
  {
    Assert.False(SoulRule.ShouldBlock(ECurrencyType.EGold, ECurrencySource.EPlayerKill, 500, active: false));
    Assert.False(SoulRule.ShouldBlock(ECurrencyType.EGold, ECurrencySource.EItemPurchase, -500, active: true));
  }
}
