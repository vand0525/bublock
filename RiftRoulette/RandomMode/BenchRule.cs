using Bublock.Modules.Queue;
using RiftRoulette.Lobby;

namespace RiftRoulette.RandomMode;

public static class BenchRule
{
  public const int MinPlayers = 3;

  // Rotation order is "next to sit out first"; new players join the back, so they play before sitting out.
  public static ulong? Next(PlayerQueue rotation, IReadOnlyCollection<ulong> players)
  {
    rotation.RemoveWhere(steamId => !players.Contains(steamId));

    foreach (var steamId in players.OrderBy(id => id))
      rotation.Join(steamId);

    if (players.Count < MinPlayers || players.Count % 2 == 0)
      return null;

    var benched = rotation.Items[0];
    rotation.MoveToBack(benched);
    return benched;
  }

  // The returner is placed as unassigned, so it fills the gap the bench player left instead of moving someone else.
  public static IReadOnlyDictionary<ulong, int> FightingTeams(
    IReadOnlyDictionary<ulong, int> teams,
    ulong? benched,
    ulong? returning,
    Random rng)
  {
    var fighters = teams
      .Where(pair => pair.Key != benched)
      .ToDictionary(pair => pair.Key, pair => pair.Key == returning ? 0 : pair.Value);

    return TeamBalance.Even(fighters, rng);
  }
}
