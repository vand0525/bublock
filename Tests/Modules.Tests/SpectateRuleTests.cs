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
  public void LookDown_is_pitch_89_and_keeps_the_yaw()
  {
    var angle = SpectateRule.LookDown(135f);

    Assert.Equal(89f, angle.X);
    Assert.Equal(135f, angle.Y);
    Assert.Equal(0f, angle.Z);
  }
}
