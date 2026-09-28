namespace Bublock.Modules.Session;

public sealed class Scoreboard
{
  private readonly Dictionary<ulong, int> _points = [];

  public int Count => _points.Count;

  public IReadOnlyDictionary<ulong, int> Points => _points;

  public int PointsOf(ulong steamId) => _points.GetValueOrDefault(steamId);

  public int Add(ulong steamId, int points = 1)
  {
    var total = PointsOf(steamId) + points;
    _points[steamId] = total;
    return total;
  }

  // 1 + the number of players with more points: ties share a place, an unknown player is last.
  public int PlaceOf(ulong steamId)
  {
    var points = PointsOf(steamId);
    return 1 + _points.Values.Count(other => other > points);
  }

  public IReadOnlyList<(ulong SteamId, int Points)> Standings() =>
    _points.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value)).ToList();

  public void Forget(ulong steamId) => _points.Remove(steamId);

  public void Clear() => _points.Clear();
}
