namespace RiftRoulette.Stats;

public sealed record PlayerStats(int Kills, int Deaths, int Assists)
{
  public static readonly PlayerStats Empty = new(0, 0, 0);

  public int Score => Kills + Assists - Deaths;

  public string Line => $"{Kills} / {Deaths} / {Assists}";
}

public readonly record struct Participant(ulong SteamId, int Team);

public sealed class StatsLedger
{
  private readonly Dictionary<ulong, PlayerStats> _stats = [];

  public int Count => _stats.Count;

  public PlayerStats Get(ulong steamId) => _stats.GetValueOrDefault(steamId, PlayerStats.Empty);

  public int? RecordDeath(Participant victim, Participant? attacker, IEnumerable<Participant> assisters)
  {
    Bump(victim.SteamId, deaths: 1);

    if (attacker is not { } killer || killer.SteamId == 0 || killer.SteamId == victim.SteamId || killer.Team == victim.Team)
      return null;

    Bump(killer.SteamId, kills: 1);

    var credited = new HashSet<ulong> { killer.SteamId, victim.SteamId };

    foreach (var assister in assisters)
    {
      if (assister.SteamId == 0 || assister.Team != killer.Team || !credited.Add(assister.SteamId))
        continue;

      Bump(assister.SteamId, assists: 1);
    }

    return killer.Team;
  }

  public PlayerStats Total(IEnumerable<ulong> steamIds) =>
    steamIds.Select(Get).Aggregate(
      PlayerStats.Empty,
      (sum, stats) => new PlayerStats(sum.Kills + stats.Kills, sum.Deaths + stats.Deaths, sum.Assists + stats.Assists));

  public void Reset() => _stats.Clear();

  private void Bump(ulong steamId, int kills = 0, int deaths = 0, int assists = 0)
  {
    var current = Get(steamId);
    _stats[steamId] = new PlayerStats(current.Kills + kills, current.Deaths + deaths, current.Assists + assists);
  }
}
