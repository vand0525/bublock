using System.Numerics;
using Bublock.Modules.Movement;

namespace Bublock.Tests.Modules;

public class MovementLocationTests
{
  private static void Near(Vector3 expected, Vector3 actual) =>
    Assert.True(Vector3.Distance(expected, actual) < 0.01f, $"Expected {expected}, got {actual}");

  [Fact]
  public void Offset_at_yaw_0_maps_forward_to_x_and_right_to_minus_y()
  {
    var anchor = new MovementLocation("a", new Vector3(100f, 200f, 300f), Vector3.Zero);

    Near(new Vector3(110f, 180f, 305f), anchor.Offset(new Vector3(10f, 20f, 5f)).Position);
  }

  [Fact]
  public void Offset_at_yaw_90_maps_forward_to_y_and_right_to_x()
  {
    var anchor = new MovementLocation("a", Vector3.Zero, new Vector3(0f, 90f, 0f));

    Near(new Vector3(20f, 10f, 0f), anchor.Offset(new Vector3(10f, 20f, 0f)).Position);
  }

  [Fact]
  public void Offset_at_yaw_minus_135_points_forward_down_both_axes()
  {
    var anchor = new MovementLocation("a", Vector3.Zero, new Vector3(-23f, -135f, 0f));
    var h = MathF.Sqrt(0.5f) * 100f;

    Near(new Vector3(-h, -h, 0f), anchor.Offset(new Vector3(100f, 0f, 0f)).Position);
    Near(new Vector3(-h, h, 0f), anchor.Offset(new Vector3(0f, 100f, 0f)).Position);
  }

  [Fact]
  public void Offset_keeps_the_angle_and_takes_an_optional_name()
  {
    var anchor = new MovementLocation("a", Vector3.Zero, new Vector3(-23f, 45f, 0f));

    var moved = anchor.Offset(new Vector3(1f, 2f, 3f), "a#1");

    Assert.Equal(anchor.Angle, moved.Angle);
    Assert.Equal("a#1", moved.Name);
    Assert.Equal("a", anchor.Offset(Vector3.One).Name);
  }

  [Theory]
  [InlineData(0f)]
  [InlineData(90f)]
  [InlineData(-135f)]
  public void LocalOf_undoes_Offset(float yaw)
  {
    var anchor = new MovementLocation("a", new Vector3(100f, -50f, 1536f), new Vector3(0f, yaw, 0f));
    var local = new Vector3(-120f, 40f, 264f);

    Near(local, anchor.LocalOf(anchor.Offset(local).Position));
  }
}
