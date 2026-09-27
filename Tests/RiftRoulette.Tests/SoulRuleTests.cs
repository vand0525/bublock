using DeadworksManaged.Api;
using RiftRoulette.GameLoop;

namespace Bublock.Tests.RiftRoulette;

public class SoulRuleTests
{
  [Theory]
  [InlineData(ECurrencySource.EPlayerKill)]
  [InlineData(ECurrencySource.EPlayerKillAssist)]
  [InlineData(ECurrencySource.EOrbLaneTrooper)]
  [InlineData(ECurrencySource.ETeamBonus)]
  [InlineData(ECurrencySource.EPlayerKillComeback)]
  public void Earned_souls_are_blocked_during_a_match(ECurrencySource source)
  {
    Assert.True(SoulRule.ShouldBlock(ECurrencyType.EGold, source, 150, matchRunning: true));
  }

  [Theory]
  [InlineData(ECurrencySource.ECheats)]
  [InlineData(ECurrencySource.EStartingAmount)]
  [InlineData(ECurrencySource.EItemSale)]
  public void Loadout_and_sale_sources_pass(ECurrencySource source)
  {
    Assert.False(SoulRule.ShouldBlock(ECurrencyType.EGold, source, 500, matchRunning: true));
  }

  [Fact]
  public void Nothing_is_blocked_without_a_match()
  {
    Assert.False(SoulRule.ShouldBlock(ECurrencyType.EGold, ECurrencySource.EPlayerKill, 150, matchRunning: false));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-300)]
  public void Spending_and_losses_pass(int amount)
  {
    Assert.False(SoulRule.ShouldBlock(ECurrencyType.EGold, ECurrencySource.EItemPurchase, amount, matchRunning: true));
  }

  [Fact]
  public void Other_currencies_pass()
  {
    Assert.False(SoulRule.ShouldBlock(ECurrencyType.EAbilityPoints, ECurrencySource.ELevelUp, 1, matchRunning: true));
  }
}
