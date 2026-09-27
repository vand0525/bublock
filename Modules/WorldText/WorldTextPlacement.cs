using System.Numerics;

namespace Bublock.Modules.WorldText;

public static class WorldTextPlacement
{
  public const float DefaultDistance = 150f;

  public static Vector3 InFrontOf(Vector3 eyePosition, float viewerYaw, float distance = DefaultDistance)
  {
    var radians = viewerYaw * MathF.PI / 180f;
    return eyePosition + new Vector3(MathF.Cos(radians), MathF.Sin(radians), 0f) * distance;
  }

  public static Vector3 FacingViewer(float viewerYaw) =>
    new(0f, NormalizeYaw(viewerYaw - 90f), 90f);

  public static float NormalizeYaw(float yaw)
  {
    var normalized = yaw % 360f;

    if (normalized > 180f)
      normalized -= 360f;
    else if (normalized <= -180f)
      normalized += 360f;

    return normalized;
  }
}
