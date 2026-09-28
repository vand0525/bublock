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

  private static readonly DateTime Now = new(2026, 9, 28, 5, 40, 0, DateTimeKind.Utc);

  private static FlyCamAction Step(
    bool confirmed = false, bool parkSent = false, bool atSpot = false, bool moved = false, double? seenSecondsAgo = null) =>
    SpectateRule.FlyCamStep(
      confirmed, parkSent, atSpot, moved, seenSecondsAgo is { } ago ? Now.AddSeconds(-ago) : null, Now, SpectateRule.FlyCamSettle);

  [Fact]
  public void FlyCamStep_waits_on_first_sight()
  {
    Assert.Equal(FlyCamAction.Wait, Step());
  }

  [Fact]
  public void FlyCamStep_parks_after_staying_still_for_the_settle()
  {
    Assert.Equal(FlyCamAction.Wait, Step(seenSecondsAgo: 1));
    Assert.Equal(FlyCamAction.Park, Step(seenSecondsAgo: 1.5));
    Assert.Equal(FlyCamAction.Park, Step(seenSecondsAgo: 2));
  }

  [Fact]
  public void FlyCamStep_moving_while_settling_is_manual()
  {
    Assert.Equal(FlyCamAction.Manual, Step(moved: true, seenSecondsAgo: 2));
  }

  [Fact]
  public void FlyCamStep_after_a_park_confirms_at_the_spot_or_settles_again()
  {
    Assert.Equal(FlyCamAction.Stay, Step(parkSent: true, atSpot: true, moved: true, seenSecondsAgo: 2));
    Assert.Equal(FlyCamAction.Wait, Step(parkSent: true, moved: true, seenSecondsAgo: 2));
  }

  [Fact]
  public void FlyCamStep_confirmed_stays_at_the_spot_and_is_manual_once_flown_away()
  {
    Assert.Equal(FlyCamAction.Stay, Step(confirmed: true, atSpot: true));
    Assert.Equal(FlyCamAction.Manual, Step(confirmed: true, atSpot: true, moved: true));
    Assert.Equal(FlyCamAction.Manual, Step(confirmed: true));
  }

  [Fact]
  public void FlyCamSettle_is_under_one_camera_tick()
  {
    Assert.True(SpectateRule.FlyCamSettle < TimeSpan.FromSeconds(2));
  }
}
