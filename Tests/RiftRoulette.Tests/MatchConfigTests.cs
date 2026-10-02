using RiftRoulette.GameLoop;

namespace Bublock.Tests.RiftRoulette;

public class MatchConfigTests
{
  [Theory]
  [InlineData("random", HeroMode.Random)]
  [InlineData(" Random ", HeroMode.Random)]
  [InlineData("Mirror", HeroMode.Mirror)]
  [InlineData("MIRROR", HeroMode.Mirror)]
  public void TryParseHeroMode_accepts_names_case_insensitively(string text, HeroMode expected)
  {
    Assert.True(MatchConfig.TryParseHeroMode(text, out var mode));
    Assert.Equal(expected, mode);
  }

  [Theory]
  [InlineData("")]
  [InlineData("0")]
  [InlineData("1")]
  [InlineData("-1")]
  [InlineData("chaos")]
  [InlineData("2v2")]
  [InlineData("draft")]
  [InlineData("duel")]
  [InlineData("1v1")]
  public void TryParseHeroMode_rejects_numbers_and_unknown_names(string text)
  {
    Assert.False(MatchConfig.TryParseHeroMode(text, out _));
  }

  [Fact]
  public void TryParseFormat_accepts_continuous_only()
  {
    Assert.True(MatchConfig.TryParseFormat("continuous", out var format));
    Assert.Equal(MatchFormat.Continuous, format);
    Assert.False(MatchConfig.TryParseFormat("bestof3", out _));
  }

  [Fact]
  public void Names_and_Describe_are_lowercase()
  {
    Assert.Equal("random|mirror", MatchConfig.Names<HeroMode>());
    Assert.Equal("Mode=mirror | Format=continuous", MatchConfig.Describe(HeroMode.Mirror, MatchFormat.Continuous));
  }
}
