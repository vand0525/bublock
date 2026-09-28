using Bublock.Modules.Spectate;

namespace Bublock.Tests.Modules;

public class SpectateRuleTests
{
  [Fact]
  public void Keeps_a_live_current_target()
  {
    var choice = SpectateRule.Choose(20, 30, [10, 20, 30]);

    Assert.Equal(new SpectateChoice(SpectateReason.Keep, 20), choice);
  }

  [Fact]
  public void Picks_the_killer_when_the_current_target_is_gone()
  {
    var choice = SpectateRule.Choose(20, 30, [10, 30]);

    Assert.Equal(new SpectateChoice(SpectateReason.Killer, 30), choice);
  }

  [Fact]
  public void Picks_any_when_there_is_no_killer_or_the_killer_is_not_a_candidate()
  {
    Assert.Equal(new SpectateChoice(SpectateReason.Any, 10), SpectateRule.Choose(20, null, [10, 30]));
    Assert.Equal(new SpectateChoice(SpectateReason.Any, 10), SpectateRule.Choose(20, 99, [10, 30]));
    Assert.Equal(new SpectateChoice(SpectateReason.Any, 10), SpectateRule.Choose(null, null, [10]));
  }

  [Fact]
  public void Never_picks_the_dead_victim()
  {
    var choice = SpectateRule.Choose(20, 20, [10]);

    Assert.Equal(new SpectateChoice(SpectateReason.Any, 10), choice);
  }

  [Fact]
  public void Parks_when_there_are_no_candidates()
  {
    Assert.Equal(new SpectateChoice(SpectateReason.Park, null), SpectateRule.Choose(20, 30, []));
  }

  [Theory]
  [InlineData(true, false, 0f, true)]
  [InlineData(true, false, 1500f, true)]
  [InlineData(true, false, 1501f, false)]
  [InlineData(false, false, 0f, false)]
  [InlineData(true, true, 0f, false)]
  public void ParkCheck_needs_roaming_no_target_and_near_the_spot(bool roaming, bool hasTarget, float distance, bool expected)
  {
    Assert.Equal(expected, SpectateRule.ParkCheck(roaming, hasTarget, distance, 1500f));
  }

  [Theory]
  [InlineData(true, false, 1501f, true)]
  [InlineData(true, false, 1500f, false)]
  [InlineData(false, false, 5000f, false)]
  [InlineData(true, true, 5000f, false)]
  public void IsManualMove_needs_roaming_no_target_and_past_the_tolerance(bool roaming, bool hasTarget, float distance, bool expected)
  {
    Assert.Equal(expected, SpectateRule.IsManualMove(roaming, hasTarget, distance, 1500f));
  }

  [Fact]
  public void ManualActive_until_the_hold_ends()
  {
    var now = new DateTime(2026, 9, 28, 2, 40, 0, DateTimeKind.Utc);

    Assert.False(SpectateRule.ManualActive(null, now));
    Assert.True(SpectateRule.ManualActive(now.AddSeconds(1), now));
    Assert.False(SpectateRule.ManualActive(now, now));
  }

  [Fact]
  public void FollowReady_after_the_grace_or_with_no_spawn_seen()
  {
    var now = new DateTime(2026, 9, 28, 2, 40, 0, DateTimeKind.Utc);
    var grace = TimeSpan.FromSeconds(5);

    Assert.True(SpectateRule.FollowReady(null, now, grace));
    Assert.False(SpectateRule.FollowReady(now.AddSeconds(-4), now, grace));
    Assert.True(SpectateRule.FollowReady(now.AddSeconds(-5), now, grace));
  }

  [Fact]
  public void LookDown_is_pitch_89_and_keeps_the_yaw()
  {
    var angle = SpectateRule.LookDown(135f);

    Assert.Equal(89f, angle.X);
    Assert.Equal(135f, angle.Y);
    Assert.Equal(0f, angle.Z);
  }
}
