namespace RiftRoulette.Round;

public static class WatchGuardRule
{
  public const float Margin = 300f;

  public static float Line(float spotZ) => spotZ - Margin;

  public static bool IsBelow(float z, float spotZ) => z < Line(spotZ);
}
