namespace RiftRoulette.SelfTest;

public static class EventCounters
{
  private static readonly Dictionary<string, long> Counts = [];

  public static void Hit(string name) => Counts[name] = Count(name) + 1;

  public static long Count(string name) => Counts.TryGetValue(name, out var count) ? count : 0;
}
