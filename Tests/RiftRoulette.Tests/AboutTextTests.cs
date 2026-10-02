using RiftRoulette.Lobby;

namespace Bublock.Tests.RiftRoulette;

public class AboutTextTests
{
  [Fact]
  public void Lines_explain_betting_with_the_current_numbers()
  {
    var text = string.Join("\n", AboutText.Lines(10));

    Assert.Contains("You start with 100, a kill gives 100 and an assist gives 50.", text);
    Assert.Contains("closes 10s into the round", text);
    Assert.Contains("/reserve <hero> (1,000)", text);
    Assert.Contains("/heroban <hero> (1,000)", text);
    Assert.Contains("/mark <slot> (300)", text);
    Assert.Equal(7, AboutText.Lines(10).Count);
    Assert.DoesNotContain("chip", text, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void MirrorLines_explain_the_shared_hero_without_betting()
  {
    var text = string.Join("\n", AboutText.MirrorLines());

    Assert.Contains("same hero with the same build", text);
    Assert.DoesNotContain("bet", text, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("/reserve", text);
  }
}
