using Bublock.Modules.MidBoss;

namespace Bublock.Tests.Modules;

public class MidBossRuleTests
{
  [Theory]
  [InlineData(13000, 51f, 6630)]
  [InlineData(13000, 100f, 13000)]
  [InlineData(13000, 50f, 6500)]
  [InlineData(3, 50f, 2)]
  public void HealthAtPercent_is_that_share_of_max(int max, float percent, int expected) =>
    Assert.Equal(expected, MidBossRule.HealthAtPercent(max, percent));

  [Theory]
  [InlineData(13000, 0f)]
  [InlineData(13000, -20f)]
  [InlineData(13000, 0.001f)]
  [InlineData(0, 50f)]
  public void HealthAtPercent_never_goes_below_one(int max, float percent) =>
    Assert.Equal(1, MidBossRule.HealthAtPercent(max, percent));

  [Fact]
  public void HealthAtPercent_caps_at_max_and_treats_nan_as_full()
  {
    Assert.Equal(13000, MidBossRule.HealthAtPercent(13000, 250f));
    Assert.Equal(13000, MidBossRule.HealthAtPercent(13000, float.NaN));
  }

  [Theory]
  [InlineData(1f, 1f)]
  [InlineData(3f, 3f)]
  [InlineData(0.01f, 0.1f)]
  [InlineData(-2f, 0.1f)]
  [InlineData(50f, 10f)]
  public void ClampScale_keeps_the_scale_in_range(float requested, float expected) =>
    Assert.Equal(expected, MidBossRule.ClampScale(requested), 3);

  [Fact]
  public void ClampScale_treats_nan_and_infinity_as_one()
  {
    Assert.Equal(1f, MidBossRule.ClampScale(float.NaN));
    Assert.Equal(1f, MidBossRule.ClampScale(float.PositiveInfinity));
  }

  [Fact]
  public void ScaleDamage_multiplies_by_the_clamped_scale()
  {
    Assert.Equal(60f, MidBossRule.ScaleDamage(20f, 3f), 3);
    Assert.Equal(200f, MidBossRule.ScaleDamage(20f, 99f), 3);
  }

  [Theory]
  [InlineData("off", 999999)]
  [InlineData(" OFF ", 999999)]
  [InlineData("0", 0)]
  [InlineData("30", 30)]
  public void TryParseSchedule_accepts_off_and_whole_seconds(string text, int expected)
  {
    Assert.True(MidBossRule.TryParseSchedule(text, out var seconds));
    Assert.Equal(expected, seconds);
  }

  [Theory]
  [InlineData("-1")]
  [InlineData("1.5")]
  [InlineData("soon")]
  [InlineData("")]
  [InlineData("1000000")]
  public void TryParseSchedule_refuses_anything_else(string text) =>
    Assert.False(MidBossRule.TryParseSchedule(text, out _));

  [Fact]
  public void IsBoss_matches_designer_or_class_names()
  {
    Assert.True(MidBossRule.IsBoss("npc_super_neutral", ""));
    Assert.True(MidBossRule.IsBoss("", "npc_super_neutral"));
    Assert.True(MidBossRule.IsBoss("anything", "CNPC_MidBoss"));
    Assert.False(MidBossRule.IsBoss("npc_boss_tier2", "CNPC_Boss_Tier2"));
  }

  [Fact]
  public void IsCrystal_matches_only_the_rejuv_pickup()
  {
    Assert.True(MidBossRule.IsCrystal("citadel_item_pickup_rejuv"));
    Assert.False(MidBossRule.IsCrystal("npc_super_neutral"));
  }

  [Theory]
  [InlineData(1, true)]
  [InlineData(1_000_000, true)]
  [InlineData(0, false)]
  [InlineData(1_000_001, false)]
  public void IsValidHealth_is_one_to_a_million(int health, bool expected) =>
    Assert.Equal(expected, MidBossRule.IsValidHealth(health));
}
