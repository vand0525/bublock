namespace RiftRoulette.Duel;

public sealed class StreakBoard
{
  private readonly Dictionary<ulong, int> _best = [];

  public int Count => _best.Count;

  public IReadOnlyDictionary<ulong, int> Best => _best;

  public int BestOf(ulong steamId) => _best.GetValueOrDefault(steamId);

  public bool Record(ulong steamId, int streak)
  {
    if (streak <= BestOf(steamId))
      return false;

    _best[steamId] = streak;
    return true;
  }

  public void Forget(ulong steamId) => _best.Remove(steamId);

  public void Clear() => _best.Clear();
}
