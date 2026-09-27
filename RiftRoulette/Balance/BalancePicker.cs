using RiftRoulette.Lobby;

namespace RiftRoulette.Balance;

public sealed record BalanceCandidate(ulong SteamId, int Team, int Score);

public sealed record BalanceMove(ulong Best, ulong? Weakest);

public static class BalancePicker
{
  public const int MinPlayers = 3;

  public static BalanceMove? Pick(IReadOnlyList<BalanceCandidate> players, int winningTeam)
  {
    var winners = players.Where(player => player.Team == winningTeam).ToList();
    var losers = players.Where(player => player.Team == RiftRouletteTeams.Other(winningTeam)).ToList();

    if (winners.Count == 0 || winners.Count + losers.Count < MinPlayers)
      return null;

    var best = winners.OrderByDescending(player => player.Score).ThenBy(player => player.SteamId).First();

    if (winners.Count > losers.Count)
      return new BalanceMove(best.SteamId, null);

    var weakest = losers.OrderBy(player => player.Score).ThenBy(player => player.SteamId).First();
    return new BalanceMove(best.SteamId, weakest.SteamId);
  }
}
