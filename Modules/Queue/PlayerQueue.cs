namespace Bublock.Modules.Queue;

public sealed class PlayerQueue
{
  private readonly List<ulong> _items = [];

  public int Count => _items.Count;

  public IReadOnlyList<ulong> Items => _items;

  public bool Contains(ulong steamId) => _items.Contains(steamId);

  public int? PositionOf(ulong steamId)
  {
    var index = _items.IndexOf(steamId);
    return index < 0 ? null : index + 1;
  }

  public int Join(ulong steamId)
  {
    if (!_items.Contains(steamId))
      _items.Add(steamId);

    return _items.IndexOf(steamId) + 1;
  }

  public bool Leave(ulong steamId) => _items.Remove(steamId);

  public IReadOnlyList<ulong> Front(int count) => _items.Take(Math.Max(0, count)).ToList();

  public bool MoveToBack(ulong steamId)
  {
    if (!_items.Remove(steamId))
      return false;

    _items.Add(steamId);
    return true;
  }

  public int RemoveWhere(Func<ulong, bool> predicate) => _items.RemoveAll(id => predicate(id));

  public void Clear() => _items.Clear();
}
