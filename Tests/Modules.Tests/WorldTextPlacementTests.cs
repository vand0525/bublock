using System.Numerics;
using Bublock.Modules.WorldText;

namespace Bublock.Tests.Modules;

public class WorldTextPlacementTests
{
  private const float Tolerance = 0.001f;

  [Theory]
  [InlineData(0f, 150f, 0f)]
  [InlineData(90f, 0f, 150f)]
  [InlineData(180f, -150f, 0f)]
  [InlineData(-90f, 0f, -150f)]
  public void InFrontOf_moves_along_yaw_and_stays_level(float yaw, float dx, float dy)
  {
    var eye = new Vector3(10f, 20f, 72f);

    var point = WorldTextPlacement.InFrontOf(eye, yaw);

    Assert.Equal(eye.X + dx, point.X, Tolerance);
    Assert.Equal(eye.Y + dy, point.Y, Tolerance);
    Assert.Equal(eye.Z, point.Z, Tolerance);
  }

  [Fact]
  public void InFrontOf_uses_custom_distance()
  {
    var point = WorldTextPlacement.InFrontOf(Vector3.Zero, 0f, 500f);

    Assert.Equal(500f, point.X, Tolerance);
  }

  [Fact]
  public void FacingViewer_matches_archive_sapphire_board()
  {
    // Sapphire board sits at +Y from draft; viewer looks along yaw 90; archive yaw 360 (= 0).
    Assert.Equal(new Vector3(0f, 0f, 90f), WorldTextPlacement.FacingViewer(90f));
  }

  [Fact]
  public void FacingViewer_matches_archive_amber_board()
  {
    // Amber board sits at -Y from draft; viewer looks along yaw -90; archive yaw 180.
    Assert.Equal(new Vector3(0f, 180f, 90f), WorldTextPlacement.FacingViewer(-90f));
  }

  [Theory]
  [InlineData(0f, 0f)]
  [InlineData(180f, 180f)]
  [InlineData(-180f, 180f)]
  [InlineData(360f, 0f)]
  [InlineData(270f, -90f)]
  [InlineData(-270f, 90f)]
  [InlineData(725f, 5f)]
  public void NormalizeYaw_wraps_into_half_open_range(float yaw, float expected)
  {
    Assert.Equal(expected, WorldTextPlacement.NormalizeYaw(yaw), Tolerance);
  }
}
