using System.Numerics;
using RiftRoulette.Rift;

namespace RiftRoulette.Round;

public static class WatchLayout
{
  public static readonly Vector3 WelcomeOffset = new(500f, 500f, 300f);

  public static Vector3 Offset(RiftSide side, Vector3 greenOffset) =>
    side == RiftSide.Green ? greenOffset : new Vector3(-greenOffset.X, -greenOffset.Y, greenOffset.Z);

  public static float Yaw(RiftSide side, float greenYaw) =>
    side == RiftSide.Green ? greenYaw : Normalize(greenYaw + 180f);

  public static Vector3 Angle(RiftSide side, Vector3 greenAngle) =>
    new(greenAngle.X, Yaw(side, greenAngle.Y), greenAngle.Z);

  public static Vector3 LookAt(Vector3 from, Vector3 to)
  {
    var delta = to - from;
    var flat = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
    var yaw = MathF.Atan2(delta.Y, delta.X) * 180f / MathF.PI;
    var pitch = -MathF.Atan2(delta.Z, flat) * 180f / MathF.PI;

    return new Vector3(pitch, Normalize(yaw), 0f);
  }

  public static float Normalize(float yaw)
  {
    var wrapped = yaw % 360f;

    if (wrapped > 180f)
      wrapped -= 360f;
    else if (wrapped <= -180f)
      wrapped += 360f;

    return wrapped;
  }
}
