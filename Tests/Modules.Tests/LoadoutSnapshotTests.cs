using Bublock.Modules.Loadout;
using DeadworksManaged.Api;

namespace Bublock.Tests.Modules;

public class LoadoutSnapshotTests
{
  [Fact]
  public void HeldLines_lists_a_summary_then_each_item_with_cost_and_imbues()
  {
    var snapshot = new LoadoutSnapshot(
      Heroes.Haze,
      25,
      2,
      0,
      [new SnapshotAbility("ability_x", EAbilitySlot.Signature1, 0b111)],
      [new SnapshotItem("cheap", []), new SnapshotItem("big", ["ability_x"])],
      "Theo");

    var lines = snapshot.HeldLines(item => item == "big" ? 6400 : 800);

    Assert.Equal(3, lines.Count);
    Assert.StartsWith("Hero=Haze | Level=25 | Items=2 | Value=7200 | AP=2 | Unlocks=0 | Abilities=Signature1:111", lines[0]);
    Assert.Equal("1. cheap (800)", lines[1]);
    Assert.Equal("2. big (6400) imbued ability_x", lines[2]);
  }
}
