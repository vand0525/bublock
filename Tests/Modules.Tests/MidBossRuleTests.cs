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

  [Theory]
  [InlineData(0, true)]
  [InlineData(1_000_000, true)]
  [InlineData(-1, false)]
  [InlineData(1_000_001, false)]
  public void IsValidSouls_is_zero_to_a_million(int souls, bool expected) =>
    Assert.Equal(expected, MidBossRule.IsValidSouls(souls));

  [Fact]
  public void MinuteBySouls_rises_with_souls()
  {
    var table = MidBossRule.MinuteBySouls;

    Assert.Equal((0, 0f), table[0]);

    for (var i = 1; i < table.Length; i++)
    {
      Assert.True(table[i].Souls > table[i - 1].Souls);
      Assert.True(table[i].Minute > table[i - 1].Minute);
    }
  }

  [Theory]
  [InlineData(-500, 0f)]
  [InlineData(0, 0f)]
  [InlineData(20000, 21.1f)]
  [InlineData(21250, 22.05f)]
  [InlineData(45000, 38.9f)]
  [InlineData(47500, 40.7f)]
  public void MinuteForSouls_interpolates_and_extends_the_last_slope(int souls, float expected) =>
    Assert.Equal(expected, MidBossRule.MinuteForSouls(souls), 2);

  [Theory]
  [InlineData(0f, 13000)]
  [InlineData(-3f, 13000)]
  [InlineData(15.98f, 16120)]
  [InlineData(23.43f, 17485)]
  [InlineData(100000f, 1_000_000)]
  public void HealthAtMinute_matches_the_game_and_rounds_the_minute(float minute, int expected) =>
    Assert.Equal(expected, MidBossRule.HealthAtMinute(minute));

  [Theory]
  [InlineData(0, 13000)]
  [InlineData(20000, 17095)]
  [InlineData(45000, 20605)]
  public void HealthForSouls_is_the_boss_spawned_at_that_minute(int souls, int expected) =>
    Assert.Equal(expected, MidBossRule.HealthForSouls(souls));

  [Theory]
  [InlineData(12, 1f)]
  [InlineData(6, 2f)]
  [InlineData(2, 6f)]
  [InlineData(24, 0.5f)]
  [InlineData(1, 10f)]
  [InlineData(0, 10f)]
  [InlineData(500, 0.1f)]
  public void ScaleForFighters_is_twelve_over_fighters_clamped(int fighters, float expected) =>
    Assert.Equal(expected, MidBossRule.ScaleForFighters(fighters), 3);

  [Theory]
  [InlineData(0, 0, 1, 0, 2)]
  [InlineData(2, 0, 3, 0, 2)]
  [InlineData(0, 0, 0, 1, 3)]
  [InlineData(1, 1, 2, 2, 2)]
  [InlineData(3, 0, 0, 0, 0)]
  [InlineData(1, 1, 1, 1, 0)]
  public void CrystalTeam_is_the_team_whose_count_rose(int amberBefore, int sapphireBefore, int amberNow, int sapphireNow, int expected) =>
    Assert.Equal(expected, MidBossRule.CrystalTeam((amberBefore, sapphireBefore), (amberNow, sapphireNow)));
}
