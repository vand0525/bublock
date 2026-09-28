using System.Numerics;
using Bublock.Modules.Movement;
using RiftRoulette.Rift;

namespace RiftRoulette.Round;

public static class WatchLayout
{
  public static readonly Vector3 WelcomeOffset = new(500f, 500f, 300f);

  // The welcome text starts at WelcomeOffset (its bottom-left) and runs toward -y on green, facing -x.
  // Estimated from the font size (64 px, 3 units per px), then nudged by eye in game; not measured.
  public const float WelcomeHalfWidth = 730f;
  public const float WelcomeHalfHeight = 70f;
  public const float WelcomeViewDistance = 750f;
  public const float EyeHeight = 64f;

  public static readonly Vector3 WelcomeCenter = WelcomeOffset + new Vector3(0f, -WelcomeHalfWidth, WelcomeHalfHeight);

  public static readonly Vector3 WelcomeFrontOffset = new(WelcomeCenter.X - WelcomeViewDistance, WelcomeCenter.Y, 0f);

  public static MovementLocation WelcomeFront(MovementLocation anchor, RiftSide side)
  {
    var position = anchor.Position + Offset(side, WelcomeFrontOffset);
    var center = anchor.Position + Offset(side, WelcomeCenter);

    return new MovementLocation($"{anchor.Name}#welcome", position, LookAt(position + Vector3.UnitZ * EyeHeight, center));
  }

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
