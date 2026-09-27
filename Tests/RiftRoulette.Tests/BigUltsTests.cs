using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class BigUltsTests
{
  [Theory]
  [InlineData("citadel_ability_lash_ultimate")]
  [InlineData("citadel_ability_storm_cloud")]
  [InlineData("synth_affliction")]
  [InlineData("CITADEL_ABILITY_LASH_ULTIMATE")]
  public void Listed_ults_are_big(string ability)
  {
    Assert.True(BigUlts.IsBig(ability));
  }

  [Theory]
  [InlineData("citadel_ability_shiv_killing_blow")]
  [InlineData("ability_melee_lash")]
  [InlineData("")]
  [InlineData(null)]
  public void Other_abilities_are_not(string? ability)
  {
    Assert.False(BigUlts.IsBig(ability));
  }

  [Fact]
  public void Every_entry_names_a_hero()
  {
    Assert.Equal(17, BigUlts.Heroes.Count);
    Assert.All(BigUlts.Heroes.Values, hero => Assert.False(string.IsNullOrWhiteSpace(hero)));
  }
}
