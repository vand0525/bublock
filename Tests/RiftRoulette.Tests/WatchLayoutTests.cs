using System.Numerics;
using RiftRoulette.Rift;
using RiftRoulette.Round;

namespace Bublock.Tests.RiftRoulette;

public class WatchLayoutTests
{
  private const float Tolerance = 0.5f;

  [Fact]
  public void Green_offset_and_yaw_are_unchanged()
  {
    var offset = new Vector3(500f, 500f, 300f);

    Assert.Equal(offset, WatchLayout.Offset(RiftSide.Green, offset));
    Assert.Equal(-90f, WatchLayout.Yaw(RiftSide.Green, -90f));
  }

  [Fact]
  public void Yellow_offset_is_rotated_half_a_turn()
  {
    Assert.Equal(new Vector3(-500f, -500f, 300f), WatchLayout.Offset(RiftSide.Yellow, new Vector3(500f, 500f, 300f)));
  }

  [Fact]
  public void Yellow_welcome_board_sits_toward_the_map_edge()
  {
    var spot = RoundLocations.WatchFor(RiftSide.Yellow).Position;
    var board = spot + WatchLayout.Offset(RiftSide.Yellow, WatchLayout.WelcomeOffset);

    Assert.True(board.X < spot.X);
    Assert.True(board.X < -7560f);
  }

  [Theory]
  [InlineData(-90f, 90f)]
  [InlineData(360f, 180f)]
  [InlineData(180f, 0f)]
  public void Yellow_yaw_is_green_plus_180(float green, float yellow)
  {
    Assert.Equal(yellow, WatchLayout.Yaw(RiftSide.Yellow, green), Tolerance);
  }

  [Fact]
  public void Angle_changes_only_the_yaw()
  {
    Assert.Equal(new Vector3(0f, 90f, 90f), WatchLayout.Angle(RiftSide.Yellow, new Vector3(0f, -90f, 90f)));
  }

  [Theory]
  [InlineData(RiftSide.Green)]
  [InlineData(RiftSide.Yellow)]
  [InlineData(RiftSide.Center)]
  public void Watch_view_angle_points_at_the_welcome_board(RiftSide side)
  {
    var spot = RoundLocations.WatchFor(side);
    var board = spot.Position + WatchLayout.Offset(side, WatchLayout.WelcomeOffset);
    var expected = WatchLayout.LookAt(spot.Position, board);

    Assert.Equal(expected.X, spot.Angle.X, Tolerance);
    Assert.Equal(0f, WatchLayout.Normalize(expected.Y - spot.Angle.Y), Tolerance);
  }

  [Theory]
  [InlineData(RiftSide.Green, 0f)]
  [InlineData(RiftSide.Yellow, 180f)]
  [InlineData(RiftSide.Center, 0f)]
  public void Welcome_front_faces_the_sign_head_on(RiftSide side, float yaw)
  {
    var anchor = RoundLocations.WatchFor(side);
    var front = WatchLayout.WelcomeFront(anchor, side);
    var center = anchor.Position + WatchLayout.Offset(side, WatchLayout.WelcomeCenter);

    Assert.Equal(0f, WatchLayout.Normalize(front.Angle.Y - yaw), Tolerance);
    Assert.True(front.Angle.X < 0f);
    Assert.Equal(anchor.Position.Z, front.Position.Z, Tolerance);
    Assert.Equal(center.Y, front.Position.Y, Tolerance);
    Assert.Equal(WatchLayout.WelcomeViewDistance, MathF.Abs(center.X - front.Position.X), Tolerance);
  }

  [Fact]
  public void Center_offset_matches_green()
  {
    var offset = new Vector3(500f, 500f, 300f);

    Assert.Equal(offset, WatchLayout.Offset(RiftSide.Center, offset));
    Assert.Equal(-90f, WatchLayout.Yaw(RiftSide.Center, -90f));
  }

  [Fact]
  public void LookAt_pitches_up_for_a_target_above()
  {
    var angle = WatchLayout.LookAt(Vector3.Zero, new Vector3(100f, 0f, 100f));

    Assert.Equal(-45f, angle.X, Tolerance);
    Assert.Equal(0f, angle.Y, Tolerance);
  }
}
