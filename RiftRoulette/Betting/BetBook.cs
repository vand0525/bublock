namespace RiftRoulette.Betting;

public enum BetResult
{
  Placed,
  Changed,
  NoChips,
  NotAllowed
}

public enum BetOutcome
{
  Won,
  Lost,
  Refunded
}

public readonly record struct Bet(int Team, int Stake);

public sealed record BetSettlement(ulong SteamId, Bet Bet, BetOutcome Outcome, int Balance);

public sealed class BetBook
{
  public const int StartingChips = 100;
  public const int ChipsPerKill = 100;
  public const int ChipsPerAssist = 50;
  public const int PayoutMultiplier = 2;

  private readonly Dictionary<ulong, int> _chips = [];
  private readonly Dictionary<ulong, Bet> _bets = [];

  public int OpenBets => _bets.Count;

  public IReadOnlyDictionary<ulong, Bet> Bets => _bets;

  // Anyone not seen yet this match has the starting chips.
  public int Chips(ulong steamId) => _chips.GetValueOrDefault(steamId, StartingChips);

  public int Total(ulong steamId) => Chips(steamId) + (_bets.TryGetValue(steamId, out var bet) ? bet.Stake : 0);

  public bool TryGetBet(ulong steamId, out Bet bet) => _bets.TryGetValue(steamId, out bet);

  public int AwardKill(ulong steamId) => _chips[steamId] = Chips(steamId) + ChipsPerKill;

  public int AwardAssist(ulong steamId) => _chips[steamId] = Chips(steamId) + ChipsPerAssist;

  // Staked chips are not in the balance, so they can't be spent.
  public bool TrySpend(ulong steamId, int amount)
  {
    var chips = Chips(steamId);

    if (amount <= 0 || chips < amount)
      return false;

    _chips[steamId] = chips - amount;
    return true;
  }

  public int Refund(ulong steamId, int amount) =>
    amount <= 0 ? Chips(steamId) : _chips[steamId] = Chips(steamId) + amount;

  // A share of the total: free souls first, then the rest comes off the open stake.
  public int Steal(ulong fromId, ulong toId, int divisor)
  {
    if (divisor <= 0 || fromId == toId)
      return 0;

    var amount = Total(fromId) / divisor;

    if (amount <= 0)
      return 0;

    var chips = Chips(fromId);
    var fromChips = Math.Min(chips, amount);
    _chips[fromId] = chips - fromChips;

    if (amount > fromChips && _bets.TryGetValue(fromId, out var bet))
    {
      var stake = bet.Stake - (amount - fromChips);

      if (stake > 0)
        _bets[fromId] = bet with { Stake = stake };
      else
        _bets.Remove(fromId);
    }

    _chips[toId] = Chips(toId) + amount;
    return amount;
  }

  // All in: the whole balance is staked. A second call only changes the team.
  public BetResult Place(ulong steamId, int team, IReadOnlyCollection<int> allowedTeams)
  {
    if (!allowedTeams.Contains(team))
      return BetResult.NotAllowed;

    if (_bets.TryGetValue(steamId, out var existing))
    {
      _bets[steamId] = existing with { Team = team };
      return BetResult.Changed;
    }

    var chips = Chips(steamId);

    if (chips <= 0)
      return BetResult.NoChips;

    _chips[steamId] = 0;
    _bets[steamId] = new Bet(team, chips);
    return BetResult.Placed;
  }

  // A null winner (tie, cancelled round) refunds every stake.
  public IReadOnlyList<BetSettlement> Settle(int? winner)
  {
    var settled = new List<BetSettlement>();

    foreach (var (steamId, bet) in _bets.OrderBy(pair => pair.Key))
    {
      var outcome = winner == null ? BetOutcome.Refunded : bet.Team == winner ? BetOutcome.Won : BetOutcome.Lost;
      var payout = outcome switch
      {
        BetOutcome.Won => bet.Stake * PayoutMultiplier,
        BetOutcome.Refunded => bet.Stake,
        _ => 0
      };

      _chips[steamId] = Chips(steamId) + payout;
      settled.Add(new BetSettlement(steamId, bet, outcome, _chips[steamId]));
    }

    _bets.Clear();
    return settled;
  }

  public IReadOnlyList<BetSettlement> RefundAll() => Settle(null);

  public void Reset()
  {
    _chips.Clear();
    _bets.Clear();
  }
}
