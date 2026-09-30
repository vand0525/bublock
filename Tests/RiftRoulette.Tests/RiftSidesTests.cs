using System.Numerics;
using RiftRoulette.Rift;

namespace Bublock.Tests.RiftRoulette;

public class RiftSidesTests
{
  [Theory]
  [InlineData("green", RiftSide.Green)]
  [InlineData("GREEN", RiftSide.Green)]
  [InlineData(" Yellow ", RiftSide.Yellow)]
  [InlineData("center", RiftSide.Center)]
  [InlineData("middle", RiftSide.Center)]
  [InlineData("mid", RiftSide.Center)]
  public void TryParse_accepts_named_sides(string name, RiftSide expected)
  {
    Assert.True(RiftSides.TryParse(name, out var side));
    Assert.Equal(expected, side);
  }

  [Theory]
  [InlineData("")]
  [InlineData(null)]
  [InlineData("purple")]
  public void TryParse_rejects_other_names(string? name)
  {
    Assert.False(RiftSides.TryParse(name, out _));
  }

  [Fact]
  public void Other_flips_green_and_yellow()
  {
    Assert.Equal(RiftSide.Yellow, RiftSides.Other(RiftSide.Green));
    Assert.Equal(RiftSide.Green, RiftSides.Other(RiftSide.Yellow));
  }

  [Theory]
  [InlineData(RiftSide.Green, true, RiftSide.Yellow)]
  [InlineData(RiftSide.Yellow, true, RiftSide.Center)]
  [InlineData(RiftSide.Center, true, RiftSide.Green)]
  [InlineData(RiftSide.Green, false, RiftSide.Yellow)]
  [InlineData(RiftSide.Yellow, false, RiftSide.Green)]
  [InlineData(RiftSide.Center, false, RiftSide.Green)]
  public void NextInRotation_follows_mid_toggle(RiftSide spawned, bool mid, RiftSide expected)
  {
    Assert.Equal(expected, RiftSides.NextInRotation(spawned, mid));
  }

  [Fact]
  public void Name_uses_archive_spelling()
  {
    Assert.Equal("GREEN", RiftSides.Name(RiftSide.Green));
    Assert.Equal("YELLOW", RiftSides.Name(RiftSide.Yellow));
    Assert.Equal("CENTER", RiftSides.Name(RiftSide.Center));
  }

  [Theory]
  [InlineData(7612f, -0.000661f, 444f, RiftSide.Green)]
  [InlineData(-7560f, 0f, 424f, RiftSide.Yellow)]
  [InlineData(7400f, 300f, 500f, RiftSide.Green)]
  [InlineData(-7800f, -250f, 380f, RiftSide.Yellow)]
  [InlineData(0f, 0f, 448f, RiftSide.Center)]
  [InlineData(100f, -50f, 500f, RiftSide.Center)]
  public void TryMatch_finds_the_side_near_a_rift(float x, float y, float z, RiftSide expected)
  {
    Assert.True(RiftSides.TryMatch(new Vector3(x, y, z), out var side));
    Assert.Equal(expected, side);
  }

  [Theory]
  [InlineData(7612f, 5000f, 444f)]
  [InlineData(-20000f, 0f, 424f)]
  public void TryMatch_rejects_positions_away_from_all_rifts(float x, float y, float z)
  {
    Assert.False(RiftSides.TryMatch(new Vector3(x, y, z), out _));
  }

  [Theory]
  [InlineData(7612f, -0.000661f, 444f, RiftSide.Green)]
  [InlineData(-7560f, 0f, 424f, RiftSide.Yellow)]
  [InlineData(0f, 0f, 448f, RiftSide.Center)]
  [InlineData(500f, 0f, 0f, RiftSide.Center)]
  [InlineData(-7000f, -6000f, 900f, RiftSide.Yellow)]
  public void Nearest_picks_the_closer_rift(float x, float y, float z, RiftSide expected)
  {
    Assert.Equal(expected, RiftSides.Nearest(new Vector3(x, y, z)));
  }

  [Fact]
  public void Positions_match_the_archive_and_mid_height()
  {
    Assert.Equal(new Vector3(7612f, -0.000661f, 444f), RiftSides.Position(RiftSide.Green));
    Assert.Equal(new Vector3(-7560f, 0f, 424f), RiftSides.Position(RiftSide.Yellow));
    Assert.Equal(new Vector3(0f, 0f, 448f), RiftSides.Position(RiftSide.Center));
  }
}
