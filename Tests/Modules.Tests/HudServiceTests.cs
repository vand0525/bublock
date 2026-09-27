using Bublock.Modules.Hud;

namespace Bublock.Tests.Modules;

public class HudServiceTests
{
  [Fact]
  public void ParseAnnouncement_without_bar_is_title_only()
  {
    Assert.Equal(("Round 3", ""), HudService.ParseAnnouncement("Round 3"));
  }

  [Fact]
  public void ParseAnnouncement_splits_on_first_bar_and_trims()
  {
    Assert.Equal(("Round 3", "GREEN rift | extra"), HudService.ParseAnnouncement(" Round 3 | GREEN rift | extra "));
  }

  [Fact]
  public void ParseAnnouncement_of_bar_only_is_empty()
  {
    Assert.Equal(("", ""), HudService.ParseAnnouncement("|"));
  }
}
