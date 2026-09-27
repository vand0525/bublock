using RiftRoulette.GameLoop;

namespace Bublock.Tests.RiftRoulette;

public class AutoStartRuleTests
{
  [Theory]
  [InlineData(false, false, 4)]
  [InlineData(false, true, 0)]
  public void Disabled_does_nothing(bool enabled, bool running, int humans)
  {
    Assert.Equal(AutoStartAction.None, AutoStartRule.Decide(enabled, running, humans));
  }

  [Fact]
  public void One_player_alone_does_not_start()
  {
    Assert.Equal(AutoStartAction.None, AutoStartRule.Decide(true, false, 1));
  }

  [Fact]
  public void Second_player_starts_the_match()
  {
    Assert.Equal(AutoStartAction.Start, AutoStartRule.Decide(true, false, 2));
  }

  [Fact]
  public void Running_match_with_enough_players_continues()
  {
    Assert.Equal(AutoStartAction.None, AutoStartRule.Decide(true, true, 2));
  }

  [Theory]
  [InlineData(1)]
  [InlineData(0)]
  public void Running_match_below_minimum_ends(int humans)
  {
    Assert.Equal(AutoStartAction.End, AutoStartRule.Decide(true, true, humans));
  }

  [Fact]
  public void Disconnect_never_starts_a_match()
  {
    Assert.Equal(AutoStartAction.None, AutoStartRule.Decide(true, false, 3, leaving: true));
  }

  [Fact]
  public void Disconnect_still_ends_a_match()
  {
    Assert.Equal(AutoStartAction.End, AutoStartRule.Decide(true, true, 1, leaving: true));
  }

  [Fact]
  public void Custom_minimum_is_respected()
  {
    Assert.Equal(AutoStartAction.None, AutoStartRule.Decide(true, false, 3, minPlayers: 4));
    Assert.Equal(AutoStartAction.Start, AutoStartRule.Decide(true, false, 4, minPlayers: 4));
  }
}
