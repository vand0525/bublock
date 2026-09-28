using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class PauseRuleTests
{
  [Theory]
  [InlineData("pause", true)]
  [InlineData("SETPAUSE", true)]
  [InlineData(" citadel_pause ", true)]
  [InlineData("citadel_toggle_server_pause", true)]
  [InlineData("citadel_pause_count", false)]
  [InlineData("unstick", false)]
  [InlineData("", false)]
  [InlineData(null, false)]
  public void IsPauseCommand_matches_only_pause_commands(string? command, bool expected)
  {
    Assert.Equal(expected, PauseRule.IsPauseCommand(command));
  }

  [Fact]
  public void ConVars_turn_pausing_off_and_never_touch_pause_counts()
  {
    var off = PauseRule.ConVars(false);
    var on = PauseRule.ConVars(true);

    Assert.All(off, convar => Assert.Equal(0, convar.Value));
    Assert.Contains(("citadel_allow_pausing", 1), on);
    Assert.Contains(("citadel_allow_pause_in_match", 1), on);
    Assert.Contains(("citadel_pause_allow_in_pregame", 0), on);
    Assert.DoesNotContain(off.Concat(on), convar => convar.Name.Contains("count") || convar.Name.Contains("allowed"));
  }

  [Theory]
  [InlineData(true, false, 10_000, null, true)]
  [InlineData(true, false, 10_000, 7_000L, true)]
  [InlineData(true, false, 10_000, 9_000L, false)]
  [InlineData(false, false, 10_000, null, false)]
  [InlineData(true, true, 10_000, null, false)]
  public void ShouldUnpause_only_when_paused_off_and_not_tried_recently(bool paused, bool allowed, long now, long? last, bool expected)
  {
    Assert.Equal(expected, PauseRule.ShouldUnpause(paused, allowed, now, last));
  }

  [Theory]
  [InlineData(10_000, null, true)]
  [InlineData(10_000, 4_000L, true)]
  [InlineData(10_000, 8_000L, false)]
  public void ShouldTell_once_per_cooldown(long now, long? last, bool expected)
  {
    Assert.Equal(expected, PauseRule.ShouldTell(now, last));
  }
}
