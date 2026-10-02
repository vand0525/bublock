namespace RiftRoulette.Betting;

public readonly record struct Mark(ulong TargetId, string TargetName, int Round);

public sealed class MarkBook
{
  public const int Cost = 300;
  public const int StealDivisor = 4;

  private readonly Dictionary<ulong, Mark> _marks = [];

  public int Count => _marks.Count;

  public IReadOnlyDictionary<ulong, Mark> Marks => _marks;

  public bool TryGet(ulong markerId, out Mark mark) => _marks.TryGetValue(markerId, out mark);

  // One mark per marker; several markers may mark the same target.
  public bool Place(ulong markerId, ulong targetId, string targetName, int round)
  {
    if (markerId == 0 || targetId == 0 || markerId == targetId || _marks.ContainsKey(markerId))
      return false;

    _marks[markerId] = new Mark(targetId, targetName, round);
    return true;
  }

  public bool TryConsume(ulong markerId, ulong victimId, int round)
  {
    if (!_marks.TryGetValue(markerId, out var mark) || mark.TargetId != victimId || mark.Round != round)
      return false;

    _marks.Remove(markerId);
    return true;
  }

  public IReadOnlyList<(ulong Marker, Mark Mark)> TakeAll()
  {
    var taken = _marks.OrderBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value)).ToList();
    _marks.Clear();
    return taken;
  }

  public void Reset() => _marks.Clear();
}
