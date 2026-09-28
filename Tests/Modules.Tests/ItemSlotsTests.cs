using Bublock.Modules.Loadout;

namespace Bublock.Tests.Modules;

public class ItemSlotsTests
{
  [Theory]
  [InlineData(0, 9)]
  [InlineData(1000, 9)]
  [InlineData(14000, 9)]
  [InlineData(15999, 9)]
  [InlineData(16000, 10)]
  [InlineData(20000, 10)]
  [InlineData(21999, 10)]
  [InlineData(22000, 11)]
  [InlineData(27999, 11)]
  [InlineData(28000, 12)]
  [InlineData(200000, 12)]
  public void ForSouls_follows_the_walker_breakpoints(int souls, int slots)
  {
    Assert.Equal(slots, ItemSlots.ForSouls(souls));
  }

  [Theory]
  [InlineData(9, false)]
  [InlineData(11, false)]
  [InlineData(12, true)]
  public void ExtraPasses_only_with_every_slot_open(int slots, bool expected)
  {
    Assert.Equal(expected, ItemSlots.ExtraPasses(slots));
  }

  [Fact]
  public void Breakpoints_grow_and_end_at_max()
  {
    var previous = (Souls: 0, Slots: ItemSlots.BaseSlots);

    foreach (var entry in ItemSlots.Breakpoints)
    {
      Assert.True(entry.Souls > previous.Souls);
      Assert.True(entry.Slots > previous.Slots);
      previous = entry;
    }

    Assert.Equal(ItemSlots.MaxSlots, previous.Slots);
  }

  [Fact]
  public void Describe_lists_every_breakpoint()
  {
    Assert.Equal("9 items, then 10 from 16,000, 11 from 22,000, 12 from 28,000", ItemSlots.Describe());
  }
}
