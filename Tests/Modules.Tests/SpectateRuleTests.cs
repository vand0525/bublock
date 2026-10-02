using System.Numerics;
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

  [Theory]
  [InlineData(10f, 10f)]
  [InlineData(190f, -170f)]
  [InlineData(-190f, 170f)]
  [InlineData(180f, -180f)]
  [InlineData(360f, 0f)]
  public void WrapDegrees_is_in_minus_180_to_180(float degrees, float expected)
  {
    Assert.Equal(expected, SpectateRule.WrapDegrees(degrees), 3);
  }

  [Fact]
  public void Turned_is_the_larger_of_pitch_and_wrapped_yaw()
  {
    Assert.Equal(2f, SpectateRule.Turned(new Vector3(0f, 179f, 0f), new Vector3(0f, -179f, 0f)), 3);
    Assert.Equal(10f, SpectateRule.Turned(new Vector3(80f, 0f, 0f), new Vector3(70f, 1f, 0f)), 3);
  }

  [Theory]
  [InlineData(0f, 0f, false)]
  [InlineData(50f, 3f, false)]
  [InlineData(51f, 0f, true)]
  [InlineData(0f, 3.5f, true)]
  public void HandMoved_past_the_move_or_turn_threshold(float distance, float turned, bool expected)
  {
    Assert.Equal(expected, SpectateRule.HandMoved(distance, turned));
  }
}
