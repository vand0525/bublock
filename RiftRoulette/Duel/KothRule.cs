namespace RiftRoulette.Duel;

public static class KothRule
{
  public static ulong? Winner(IReadOnlyDictionary<ulong, int> fighterTeams, int? winnerTeam)
  {
    if (winnerTeam is not { } team || Loser(fighterTeams, team) == null)
      return null;

    return fighterTeams.First(pair => pair.Value == team).Key;
  }

  public static ulong? Loser(IReadOnlyDictionary<ulong, int> fighterTeams, int? winnerTeam)
  {
    if (winnerTeam is not { } team || fighterTeams.Count(pair => pair.Value == team) != 1)
      return null;

    var losers = fighterTeams.Where(pair => pair.Value != team).Select(pair => pair.Key).ToList();
    return losers.Count == 1 ? losers[0] : null;
  }

  public static (ulong King, int Streak) Crown(ulong? king, int streak, ulong winner) =>
    king == winner ? (winner, streak + 1) : (winner, 1);
}
