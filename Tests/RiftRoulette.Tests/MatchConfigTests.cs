using RiftRoulette.GameLoop;

namespace Bublock.Tests.RiftRoulette;

public class MatchConfigTests
{
  [Theory]
  [InlineData("random", HeroMode.Random)]
  [InlineData("DRAFT", HeroMode.Draft)]
  [InlineData(" Random ", HeroMode.Random)]
  [InlineData("duel", HeroMode.Duel)]
  [InlineData("1v1", HeroMode.Duel)]
  [InlineData(" 1V1 ", HeroMode.Duel)]
  [InlineData("Mirror", HeroMode.Mirror)]
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
    Assert.Equal("random|draft|duel|mirror", MatchConfig.Names<HeroMode>());
    Assert.Equal("Mode=draft | Format=continuous", MatchConfig.Describe(HeroMode.Draft, MatchFormat.Continuous));
  }
}
