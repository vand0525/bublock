using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class AutoRestartRuleTests
{
  private const double Hour = 3600;

  [Fact]
  public void Stuck_join_on_an_empty_server_reloads()
  {
    Assert.Equal(AutoRestartRule.StuckJoin, AutoRestartRule.Reason(true, 0, 1, 0, Hour));
  }

  [Fact]
  public void Stuck_join_reloads_even_with_the_stuck_client_still_connecting()
  {
    Assert.Equal(AutoRestartRule.StuckJoin, AutoRestartRule.Reason(true, 0, 1, 1, Hour));
  }

  [Fact]
  public void Three_hours_up_on_an_empty_server_reloads()
  {
    Assert.Equal(AutoRestartRule.Uptime, AutoRestartRule.Reason(true, 0, 0, 0, 3 * Hour));
  }

  [Fact]
  public void Uptime_reload_waits_for_a_join_in_progress()
  {
    Assert.Null(AutoRestartRule.Reason(true, 0, 0, 1, 3 * Hour));
  }

  [Theory]
  [InlineData(1)]
  [InlineData(12)]
  public void Never_while_anyone_plays(int participants)
  {
    Assert.Null(AutoRestartRule.Reason(true, participants, 5, 0, 10 * Hour));
  }

  [Fact]
  public void Never_when_switched_off()
  {
    Assert.Null(AutoRestartRule.Reason(false, 0, 5, 0, 10 * Hour));
  }

  [Fact]
  public void Never_within_ten_minutes_of_a_map_start()
  {
    Assert.Null(AutoRestartRule.Reason(true, 0, 5, 0, AutoRestartRule.MinUptimeSeconds - 1));
    Assert.Equal(AutoRestartRule.StuckJoin, AutoRestartRule.Reason(true, 0, 5, 0, AutoRestartRule.MinUptimeSeconds));
  }

  [Fact]
  public void Healthy_server_under_three_hours_stays_up()
  {
    Assert.Null(AutoRestartRule.Reason(true, 0, 0, 0, 2.9 * Hour));
  }

  [Theory]
  [InlineData(179, false)]
  [InlineData(180, true)]
  public void A_join_is_stuck_after_three_minutes(double seconds, bool expected)
  {
    Assert.Equal(expected, AutoRestartRule.IsStuck(seconds));
  }
}
