namespace RiftRoulette.GunGame;

public readonly record struct LadderStep(bool Counted, int Kills, bool Won);

public sealed class GunGameLadder
{
  public const int DefaultTarget = 10;
  public const int MinTarget = 1;
  public const int MaxTarget = 50;

  private readonly Dictionary<ulong, int> _kills = [];

  public int Target { get; private set; } = DefaultTarget;

  public ulong? Winner { get; private set; }

  public IReadOnlyDictionary<ulong, int> Kills => _kills;

  public int KillsOf(ulong steamId) => _kills.GetValueOrDefault(steamId);

  // The caller credits only enemy kills by humans; the ladder only refuses self kills and kills after a win.
  public LadderStep Record(ulong attacker, ulong victim)
  {
    if (Winner != null || attacker == victim)
      return new LadderStep(false, KillsOf(attacker), false);

    var kills = KillsOf(attacker) + 1;
    _kills[attacker] = kills;

    if (kills >= Target)
      Winner = attacker;

    return new LadderStep(true, kills, Winner == attacker);
  }

  public bool TrySetTarget(int target)
  {
    if (target < MinTarget || target > MaxTarget)
      return false;

    Target = target;
    return true;
  }

  public IReadOnlyList<(ulong SteamId, int Kills)> Standings() =>
    _kills.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Select(pair => (pair.Key, pair.Value)).ToList();

  public int PlaceOf(ulong steamId)
  {
    var kills = KillsOf(steamId);
    return 1 + _kills.Values.Count(other => other > kills);
  }

  public void Forget(ulong steamId) => _kills.Remove(steamId);

  public void Reset()
  {
    _kills.Clear();
    Winner = null;
  }
}
