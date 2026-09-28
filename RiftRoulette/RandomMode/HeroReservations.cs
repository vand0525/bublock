using DeadworksManaged.Api;

namespace RiftRoulette.RandomMode;

public enum ReserveResult
{
  Holding,
  Waiting,
  AlreadyHasOne
}

public sealed record ReserveOutcome(ReserveResult Result, Heroes Hero, int Ahead, int RoundsAhead, ulong? Holder);

public sealed record ReservationPosition(Heroes Hero, int Place, int RoundsAhead, int RoundsLeft, ulong Holder);

public sealed record ReservedTurn(ulong SteamId, Heroes Hero, int Use);

public sealed class HeroReservations
{
  public const int Cost = 1000;
  public const int Rounds = 3;

  private sealed class Entry(ulong steamId)
  {
    public ulong SteamId { get; } = steamId;

    public int RoundsLeft { get; set; } = Rounds;
  }

  private readonly Dictionary<Heroes, List<Entry>> _lines = [];

  private readonly Dictionary<ulong, ReservedTurn> _taken = [];

  private int? _takenRound;

  public int Count => _lines.Values.Sum(line => line.Count);

  public ReserveOutcome TryReserve(ulong steamId, Heroes hero)
  {
    if (Position(steamId) is { } existing)
      return new ReserveOutcome(ReserveResult.AlreadyHasOne, existing.Hero, existing.Place, existing.RoundsAhead, existing.Holder);

    if (!_lines.TryGetValue(hero, out var line))
      _lines[hero] = line = [];

    var ahead = line.Count;
    var roundsAhead = line.Sum(entry => entry.RoundsLeft);
    var holder = line.FirstOrDefault()?.SteamId;

    line.Add(new Entry(steamId));
    return new ReserveOutcome(ahead == 0 ? ReserveResult.Holding : ReserveResult.Waiting, hero, ahead, roundsAhead, holder);
  }

  // For each hero, the first fighter in its line plays it and uses one round. A reroll in the same round reuses the result.
  public IReadOnlyList<ReservedTurn> Take(IReadOnlyCollection<ulong> fighters, int round)
  {
    if (_takenRound == round)
      return _taken.Values.Where(turn => fighters.Contains(turn.SteamId)).ToList();

    _takenRound = round;
    _taken.Clear();

    foreach (var (hero, line) in _lines)
    {
      var entry = line.FirstOrDefault(candidate => fighters.Contains(candidate.SteamId));

      if (entry != null)
        Use(hero, line, entry);
    }

    RemoveEmptyLines();
    return _taken.Values.ToList();
  }

  // A player who starts fighting mid-intermission gets their hero when nobody plays it this round.
  public ReservedTurn? TakeLate(ulong steamId, int round, IReadOnlySet<Heroes> playedThisRound)
  {
    if (_takenRound != round)
      return null;

    if (_taken.TryGetValue(steamId, out var already))
      return already;

    foreach (var (hero, line) in _lines)
    {
      var entry = line.FirstOrDefault(candidate => candidate.SteamId == steamId);

      if (entry == null)
        continue;

      if (playedThisRound.Contains(hero))
        return null;

      var turn = Use(hero, line, entry);
      RemoveEmptyLines();
      return turn;
    }

    return null;
  }

  public ReservationPosition? Position(ulong steamId)
  {
    foreach (var (hero, line) in _lines)
    {
      var place = line.FindIndex(entry => entry.SteamId == steamId);

      if (place < 0)
        continue;

      return new ReservationPosition(
        hero,
        place,
        line.Take(place).Sum(entry => entry.RoundsLeft),
        line[place].RoundsLeft,
        line[0].SteamId);
    }

    return null;
  }

  public void Reset()
  {
    _lines.Clear();
    _taken.Clear();
    _takenRound = null;
  }

  public static string RoundCount(int count) => count == 1 ? "1 round" : $"{count} rounds";

  public static string WaitingLine(string holderName, string heroName, int ahead, int roundsAhead) =>
    ahead <= 1
      ? $"{holderName} has reserved {heroName}. When their {RoundCount(roundsAhead)} {(roundsAhead == 1 ? "is" : "are")} done, it will be your turn."
      : $"{ahead} players are ahead of you for {heroName} ({RoundCount(roundsAhead)}). Then it will be your turn.";

  public static string TurnLine(string heroName, int use) =>
    use == 1
      ? $"Your reserved hero is up: {heroName} (round 1 of {Rounds})."
      : $"Reserved hero: {heroName} (round {use} of {Rounds}).";

  private ReservedTurn Use(Heroes hero, List<Entry> line, Entry entry)
  {
    var turn = new ReservedTurn(entry.SteamId, hero, Rounds - entry.RoundsLeft + 1);

    entry.RoundsLeft--;
    _taken[entry.SteamId] = turn;

    if (entry.RoundsLeft <= 0)
      line.Remove(entry);

    return turn;
  }

  private void RemoveEmptyLines()
  {
    foreach (var hero in _lines.Where(pair => pair.Value.Count == 0).Select(pair => pair.Key).ToList())
      _lines.Remove(hero);
  }
}
