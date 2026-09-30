using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class MapRefreshRuleTests
{
  private const int Budget = MapRefreshRule.DefaultBudget;

  [Theory]
  [InlineData(0, false)]
  [InlineData(159, false)]
  [InlineData(160, true)]
  [InlineData(200, true)]
  public void Due_at_the_budget(int fighterRounds, bool expected)
  {
    Assert.Equal(expected, MapRefreshRule.Due(fighterRounds, Budget));
  }

  [Fact]
  public void Never_due_when_switched_off()
  {
    Assert.False(MapRefreshRule.Due(10_000, 0));
  }

  [Theory]
  [InlineData(4, 40)]
  [InlineData(6, 27)]
  [InlineData(8, 20)]
  [InlineData(10, 16)]
  [InlineData(12, 14)]
  public void Rounds_left_from_map_start(int fighters, int expected)
  {
    Assert.Equal(expected, MapRefreshRule.RoundsLeft(0, Budget, fighters));
  }

  [Fact]
  public void Rounds_left_after_some_play()
  {
    Assert.Equal(1, MapRefreshRule.RoundsLeft(156, Budget, 4));
    Assert.Equal(0, MapRefreshRule.RoundsLeft(160, Budget, 4));
    Assert.Equal(0, MapRefreshRule.RoundsLeft(170, Budget, 4));
  }

  [Fact]
  public void Rounds_left_unknown_when_off_or_nobody_fights()
  {
    Assert.Null(MapRefreshRule.RoundsLeft(0, 0, 4));
    Assert.Null(MapRefreshRule.RoundsLeft(0, Budget, 0));
  }
}
