using System.Numerics;
using RiftRoulette.Boards;

namespace RiftRoulette.Tests;

public class EmptySignLayoutTests
{
  private const float Tolerance = 0.01f;

  [Fact]
  public void Camera_is_the_captured_spectator_start()
  {
    Assert.Equal(Vector3.Zero, EmptySignLayout.CameraPosition);
    Assert.Equal(new Vector3(-0.59375f, 179.3125f, 0f), EmptySignLayout.CameraAngle);
  }

  [Fact]
  public void Center_is_ahead_of_the_camera_at_the_distance_shifted_sideways()
  {
    var yaw = EmptySignLayout.CameraAngle.Y * MathF.PI / 180f;
    var forward = new Vector3(MathF.Cos(yaw), MathF.Sin(yaw), 0f);
    var right = EmptySignLayout.Right(EmptySignLayout.CameraAngle.Y);
    var offset = EmptySignLayout.Center - EmptySignLayout.CameraPosition;

    Assert.Equal(EmptySignLayout.Distance, Vector3.Dot(offset, forward), 1);
    Assert.Equal(EmptySignLayout.SideShift, Vector3.Dot(offset, right), 1);
    Assert.Equal(-MathF.Tan(EmptySignLayout.CameraAngle.X * MathF.PI / 180f) * EmptySignLayout.Distance, offset.Z, 2);
    Assert.True(offset.Z > 0f);
  }

  [Fact]
  public void Boards_face_the_camera()
  {
    var expected = new Vector3(0f, EmptySignLayout.CameraAngle.Y - 90f, 90f);

    Assert.Equal(expected, EmptySignLayout.Title().Angle);
    Assert.Equal(expected, EmptySignLayout.Join().Angle);
  }

  [Fact]
  public void Right_of_a_viewer_looking_down_minus_x_is_plus_y()
  {
    var right = EmptySignLayout.Right(180f);

    Assert.Equal(0f, right.X, 3);
    Assert.Equal(1f, right.Y, 3);
  }

  [Fact]
  public void Both_lines_are_centered_on_the_view()
  {
    AssertCentered(EmptySignLayout.Title().Position, EmptySignLayout.TitleText, EmptySignLayout.TitleScale);
    AssertCentered(EmptySignLayout.Join().Position, EmptySignLayout.JoinText, EmptySignLayout.JoinScale);
  }

  [Fact]
  public void Title_sits_above_the_join_line()
  {
    var title = EmptySignLayout.Title();
    var join = EmptySignLayout.Join();
    var joinTop = join.Position.Z + join.FontSize * join.WorldUnitsPerPx;

    Assert.True(title.Position.Z > joinTop);
    Assert.True(title.Position.Z > EmptySignLayout.Center.Z);
    Assert.True(joinTop < EmptySignLayout.Center.Z);
  }

  [Fact]
  public void Lines_read_the_title_and_the_public_address()
  {
    Assert.Equal("RIFT ROULETTE", EmptySignLayout.Title().Text);
    Assert.Equal("Join with connect 66.94.102.245:27015", EmptySignLayout.Join().Text);
  }

  private static void AssertCentered(Vector3 anchor, string text, float scale)
  {
    var right = EmptySignLayout.Right(EmptySignLayout.CameraAngle.Y);
    var middle = anchor + right * (EmptySignLayout.Width(text, scale) / 2f);
    var center = EmptySignLayout.Center;

    Assert.InRange(MathF.Abs(middle.X - center.X), 0f, Tolerance);
    Assert.InRange(MathF.Abs(middle.Y - center.Y), 0f, Tolerance);
  }
}
