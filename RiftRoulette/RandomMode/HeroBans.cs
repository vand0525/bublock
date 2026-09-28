using DeadworksManaged.Api;

namespace RiftRoulette.RandomMode;

public enum HeroBanResult
{
  Banned,
  TeamAlreadyBanned
}

public sealed record HeroBanOutcome(HeroBanResult Result, Heroes Hero, ulong SteamId);

public sealed class HeroBans
{
  public const int Cost = 1000;

  private readonly Dictionary<int, (ulong SteamId, Heroes Hero)> _pending = [];

  private HashSet<Heroes> _current = [];

  private int? _takenRound;

  public IReadOnlySet<Heroes> Current => _current;

  public int PendingCount => _pending.Count;

  // One ban per team per draw; the other team's ban is never checked, so a purchase reveals nothing about it.
  public HeroBanOutcome TryBan(int team, ulong steamId, Heroes hero)
  {
    if (_pending.TryGetValue(team, out var existing))
      return new HeroBanOutcome(HeroBanResult.TeamAlreadyBanned, existing.Hero, existing.SteamId);

    _pending[team] = (steamId, hero);
    return new HeroBanOutcome(HeroBanResult.Banned, hero, steamId);
  }

  public bool TryGetPending(int team, out Heroes hero, out ulong steamId)
  {
    var found = _pending.TryGetValue(team, out var entry);
    hero = entry.Hero;
    steamId = entry.SteamId;
    return found;
  }

  public IReadOnlyDictionary<int, Heroes> Pending() =>
    _pending.ToDictionary(pair => pair.Key, pair => pair.Value.Hero);

  // The pending bans become the draw's bans. A reroll in the same round reuses them.
  public IReadOnlySet<Heroes> Take(int round)
  {
    if (_takenRound == round)
      return _current;

    _takenRound = round;
    _current = _pending.Values.Select(entry => entry.Hero).ToHashSet();
    _pending.Clear();
    return _current;
  }

  public void Reset()
  {
    _pending.Clear();
    _current = [];
    _takenRound = null;
  }

  public static string RevealLine(IEnumerable<string> heroNames) =>
    $"Banned this round: {string.Join(", ", heroNames)}";
}
