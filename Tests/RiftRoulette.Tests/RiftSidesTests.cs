using System.Numerics;
using RiftRoulette.Rift;

namespace Bublock.Tests.RiftRoulette;

public class RiftSidesTests
{
  [Theory]
  [InlineData("green", RiftSide.Green)]
  [InlineData("GREEN", RiftSide.Green)]
  [InlineData(" Yellow ", RiftSide.Yellow)]
  public void TryParse_accepts_green_and_yellow(string name, RiftSide expected)
  {
    Assert.True(RiftSides.TryParse(name, out var side));
    Assert.Equal(expected, side);
  }

  [Theory]
  [InlineData("middle")]
  [InlineData("")]
  [InlineData(null)]
  public void TryParse_rejects_other_names(string? name)
  {
    Assert.False(RiftSides.TryParse(name, out _));
  }

  [Fact]
  public void Other_flips_the_side()
  {
    Assert.Equal(RiftSide.Yellow, RiftSides.Other(RiftSide.Green));
    Assert.Equal(RiftSide.Green, RiftSides.Other(RiftSide.Yellow));
  }

  [Fact]
  public void Name_uses_archive_spelling()
  {
    Assert.Equal("GREEN", RiftSides.Name(RiftSide.Green));
    Assert.Equal("YELLOW", RiftSides.Name(RiftSide.Yellow));
  }

  [Theory]
  [InlineData(7612f, -0.000661f, 444f, RiftSide.Green)]
  [InlineData(-7560f, 0f, 424f, RiftSide.Yellow)]
  [InlineData(7400f, 300f, 500f, RiftSide.Green)]
  [InlineData(-7800f, -250f, 380f, RiftSide.Yellow)]
  public void TryMatch_finds_the_side_near_a_rift(float x, float y, float z, RiftSide expected)
  {
    Assert.True(RiftSides.TryMatch(new Vector3(x, y, z), out var side));
    Assert.Equal(expected, side);
  }

  [Theory]
  [InlineData(0f, 0f, 0f)]
  [InlineData(7612f, 5000f, 444f)]
  [InlineData(-20000f, 0f, 424f)]
  public void TryMatch_rejects_positions_away_from_both_rifts(float x, float y, float z)
  {
    Assert.False(RiftSides.TryMatch(new Vector3(x, y, z), out _));
  }

  [Theory]
  [InlineData(7612f, -0.000661f, 444f, RiftSide.Green)]
  [InlineData(-7560f, 0f, 424f, RiftSide.Yellow)]
  [InlineData(3000f, 4000f, 0f, RiftSide.Green)]
  [InlineData(-2000f, -6000f, 900f, RiftSide.Yellow)]
  [InlineData(500f, 0f, 0f, RiftSide.Green)]
  public void Nearest_picks_the_closer_rift(float x, float y, float z, RiftSide expected)
  {
    Assert.Equal(expected, RiftSides.Nearest(new Vector3(x, y, z)));
  }

  [Fact]
  public void Positions_match_the_archive()
  {
    Assert.Equal(new Vector3(7612f, -0.000661f, 444f), RiftSides.Position(RiftSide.Green));
    Assert.Equal(new Vector3(-7560f, 0f, 424f), RiftSides.Position(RiftSide.Yellow));
  }
}
