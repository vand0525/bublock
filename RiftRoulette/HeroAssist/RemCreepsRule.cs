namespace RiftRoulette.HeroAssist;

public static class RemCreepsRule
{
  public const int DefaultCount = 3;

  public const int MaxCount = 8;

  public static bool ShouldSpawn(bool isRandom, int fighterCount, bool remIsFighter, int count) =>
    isRandom && fighterCount == 2 && remIsFighter && count > 0;

  public static int ClampCount(int count) => Math.Clamp(count, 0, MaxCount);
}
