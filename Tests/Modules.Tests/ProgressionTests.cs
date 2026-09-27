using Bublock.Modules.Loadout;

namespace Bublock.Tests.Modules;

public class ProgressionTests
{
  [Theory]
  [InlineData(0, 1, 0, 1, 0)]
  [InlineData(600, 1, 0, 1, 0)]
  [InlineData(799, 1, 0, 1, 0)]
  [InlineData(800, 2, 1, 1, 1)]
  [InlineData(3800, 8, 7, 4, 4)]
  [InlineData(18000, 24, 23, 4, 20)]
  [InlineData(20000, 25, 24, 4, 21)]
  [InlineData(49200, 36, 35, 4, 32)]
  [InlineData(100000, 36, 35, 4, 32)]
  public void ForSouls_returns_the_last_row_reached(int souls, int level, int boons, int unlocks, int points)
  {
    var reached = Progression.ForSouls(souls);

    Assert.Equal(level, reached.Level);
    Assert.Equal(boons, reached.Boons);
    Assert.Equal(unlocks, reached.Unlocks);
    Assert.Equal(points, reached.AbilityPoints);
  }

  [Fact]
  public void Max_earns_every_unlock_and_point()
  {
    Assert.Equal(36, Progression.MaxLevel);
    Assert.Equal(new ProgressionLevel(49200, 36, 35, 4, 32), Progression.Max);
  }

  [Fact]
  public void Unlocks_come_at_600_1100_2000_and_3800()
  {
    Assert.Equal(1, Progression.ForSouls(1099).Unlocks);
    Assert.Equal(2, Progression.ForSouls(1100).Unlocks);
    Assert.Equal(3, Progression.ForSouls(2000).Unlocks);
    Assert.Equal(3, Progression.ForSouls(3799).Unlocks);
    Assert.Equal(4, Progression.ForSouls(3800).Unlocks);
  }
}
