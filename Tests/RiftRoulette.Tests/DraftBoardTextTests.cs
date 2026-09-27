using DeadworksManaged.Api;
using RiftRoulette.Draft;

namespace Bublock.Tests.RiftRoulette;

public class DraftBoardTextTests
{
  private static readonly Heroes[] Heroes2 = [Heroes.Shiv, Heroes.Yamato];

  [Fact]
  public void TeamBoard_marks_selected_heroes_and_ends_with_commands()
  {
    var text = DraftBoardText.TeamBoard("SAPPHIRE", Heroes2, hero => hero == Heroes.Shiv);

    Assert.Equal("SAPPHIRE\n\nShiv (SELECTED)\nYamato\n\n/pick <hero>\n/unpick", text);
  }

  [Fact]
  public void PoolLine_marks_taken_heroes()
  {
    var line = DraftBoardText.PoolLine("Sapphire", Heroes2, hero => hero == Heroes.Yamato);

    Assert.Equal("Sapphire: Shiv, Yamato (taken)", line);
  }
}
