using System.Numerics;
using RiftRoulette.Boards;

namespace RiftRoulette.Tests;

public class EmptySignLayoutTests
{
  private const float Tolerance = 0.01f;

  private static readonly EmptySignSettings Settings = EmptySignSettings.Default;

  [Fact]
  public void Camera_is_the_captured_spectator_start()
  {
    Assert.Equal(Vector3.Zero, EmptySignLayout.CameraPosition);
    Assert.Equal(new Vector3(-0.59375f, 179.3125f, 0f), EmptySignLayout.CameraAngle);
  }

  [Fact]
  public void Center_is_ahead_at_the_distance_shifted_right_and_up()
  {
    var settings = Settings with { Distance = 500f, Shift = -120f, Height = 40f };
    var offset = EmptySignLayout.Center(settings) - EmptySignLayout.CameraPosition;
    var pitchRise = -MathF.Tan(EmptySignLayout.CameraAngle.X * MathF.PI / 180f) * settings.Distance;

    Assert.Equal(500f, Vector3.Dot(offset, Forward()), 1);
    Assert.Equal(-120f, Vector3.Dot(offset, EmptySignLayout.Right(EmptySignLayout.CameraAngle.Y)), 1);
    Assert.Equal(pitchRise + 40f, offset.Z, 2);
  }

  [Fact]
  public void Boards_face_the_camera()
  {
    var expected = new Vector3(0f, EmptySignLayout.CameraAngle.Y - 90f, 90f);

    Assert.Equal(expected, EmptySignLayout.Title(Settings).Angle);
    Assert.Equal(expected, EmptySignLayout.Join(Settings).Angle);
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
    AssertCentered(EmptySignLayout.Title(Settings).Position, Settings.TitleText, Settings.TitleScale);
    AssertCentered(EmptySignLayout.Join(Settings).Position, Settings.JoinText, Settings.JoinScale);
  }

  [Fact]
  public void Title_sits_above_the_join_line_by_the_gap()
  {
    var settings = Settings with { Gap = 20f };
    var title = EmptySignLayout.Title(settings);
    var join = EmptySignLayout.Join(settings);
    var joinTop = join.Position.Z + join.FontSize * join.WorldUnitsPerPx;
    var center = EmptySignLayout.Center(settings).Z;

    Assert.Equal(center + 20f, title.Position.Z, 2);
    Assert.Equal(center - 20f, joinTop, 2);
  }

  [Fact]
  public void Line_offsets_move_only_their_own_line()
  {
    var moved = Settings with { TitleRight = -60f, TitleUp = 25f };
    var right = EmptySignLayout.Right(EmptySignLayout.CameraAngle.Y);
    var titleMove = EmptySignLayout.Title(moved).Position - EmptySignLayout.Title(Settings).Position;

    Assert.Equal(-60f, Vector3.Dot(titleMove, right), 2);
    Assert.Equal(25f, titleMove.Z, 2);
    Assert.Equal(EmptySignLayout.Join(Settings).Position, EmptySignLayout.Join(moved).Position);

    var joinMoved = Settings with { JoinRight = 30f, JoinUp = -10f };
    var joinMove = EmptySignLayout.Join(joinMoved).Position - EmptySignLayout.Join(Settings).Position;

    Assert.Equal(30f, Vector3.Dot(joinMove, right), 2);
    Assert.Equal(-10f, joinMove.Z, 2);
    Assert.Equal(EmptySignLayout.Title(Settings).Position, EmptySignLayout.Title(joinMoved).Position);
  }

  [Fact]
  public void Lines_carry_the_settings_text_size_and_color()
  {
    var settings = Settings with { TitleText = "HELLO", TitleScale = 2f, JoinColor = new SignColor(1, 2, 3) };
    var title = EmptySignLayout.Title(settings);
    var join = EmptySignLayout.Join(settings);

    Assert.Equal("HELLO", title.Text);
    Assert.Equal(2f, title.WorldUnitsPerPx);
    Assert.Equal(new SignColor(1, 2, 3).ToWorldText(), join.Color);
  }

  private static Vector3 Forward()
  {
    var yaw = EmptySignLayout.CameraAngle.Y * MathF.PI / 180f;
    return new Vector3(MathF.Cos(yaw), MathF.Sin(yaw), 0f);
  }

  private static void AssertCentered(Vector3 anchor, string text, float scale)
  {
    var right = EmptySignLayout.Right(EmptySignLayout.CameraAngle.Y);
    var middle = anchor + right * (EmptySignLayout.Width(text, scale) / 2f);
    var center = EmptySignLayout.Center(Settings);

    Assert.InRange(MathF.Abs(middle.X - center.X), 0f, Tolerance);
    Assert.InRange(MathF.Abs(middle.Y - center.Y), 0f, Tolerance);
  }
}
